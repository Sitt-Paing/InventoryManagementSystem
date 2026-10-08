using System.Text;
using InventoryManagementSystem.Application.Common.Interfaces;
using InventoryManagementSystem.Domain.Entities;
using InventoryManagementSystem.Infrastructure.Persistence;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace InventoryManagementSystem.Infrastructure.Services.Email;

public class PurchaseOrderEmailWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RabbitMqEmailSettings _settings;
    private readonly ILogger<PurchaseOrderEmailWorker> _logger;

    public PurchaseOrderEmailWorker(IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqEmailSettings> settings, ILogger<PurchaseOrderEmailWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Email worker is disabled. Outbox requests remain pending.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _settings.Host,
                    Port = _settings.Port,
                    UserName = _settings.UserName,
                    Password = _settings.Password,
                    VirtualHost = _settings.VirtualHost,
                    AutomaticRecoveryEnabled = false,
                    ClientProvidedName = "Inventory email worker"
                };
                await using var connection = await factory.CreateConnectionAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(
                    new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true), stoppingToken);
                await channel.QueueDeclareAsync(_settings.Queue, durable: true, exclusive: false,
                    autoDelete: false, arguments: null, cancellationToken: stoppingToken);

                var nextPublish = DateTime.MinValue;
                while (!stoppingToken.IsCancellationRequested && connection.IsOpen && channel.IsOpen)
                {
                    if (DateTime.UtcNow >= nextPublish)
                    {
                        await PublishPendingAsync(channel, stoppingToken);
                        nextPublish = DateTime.UtcNow.AddSeconds(30);
                    }

                    var message = await channel.BasicGetAsync(_settings.Queue, autoAck: false, stoppingToken);
                    if (message == null)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                        continue;
                    }

                    var text = Encoding.UTF8.GetString(message.Body.Span);
                    if (Guid.TryParse(text, out var emailId))
                        await SendEmailAsync(emailId, stoppingToken);
                    else
                        _logger.LogWarning("Discarded an invalid email queue message.");

                    // Acknowledge only after the outcome was saved. On DB failure, reconnect
                    // and RabbitMQ will redeliver the unacknowledged message.
                    await channel.BasicAckAsync(message.DeliveryTag, multiple: false, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogWarning("Email worker unavailable ({ErrorType}). Retrying in 10 seconds. Pending requests are retained.",
                    exception.GetType().Name);
                try { await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            }
        }
    }

    private async Task PublishPendingAsync(IChannel channel, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryManagementDbContext>();
        var now = DateTime.UtcNow;

        // An interrupted SMTP send may already have reached the supplier. Do not
        // automatically resend these uncertain outcomes after a process crash.
        var staleBefore = DateTime.Now.AddMinutes(-10);
        var interrupted = await context.EmailOutboxes.IgnoreQueryFilters()
            .Where(x => x.Status == EmailOutboxStatus.Sending && !x.DeletedOn.HasValue
                && x.UpdatedOn < staleBefore).ToListAsync(cancellationToken);
        foreach (var email in interrupted)
            email.MarkFailed("Sending was interrupted. Verify delivery before creating another send request.");
        if (interrupted.Count > 0) await context.SaveChangesAsync(cancellationToken);

        var ids = await context.EmailOutboxes.IgnoreQueryFilters().AsNoTracking()
            .Where(x => x.Status == EmailOutboxStatus.Pending && !x.DeletedOn.HasValue
                && (!x.NextAttemptOn.HasValue || x.NextAttemptOn <= now))
            .OrderBy(x => x.CreatedOn).ThenBy(x => x.Id).Take(50)
            .Select(x => x.Id).ToListAsync(cancellationToken);

        foreach (var id in ids)
        {
            var properties = new BasicProperties { Persistent = true, MessageId = id.ToString() };
            await channel.BasicPublishAsync(exchange: "", routingKey: _settings.Queue, mandatory: true,
                basicProperties: properties, body: Encoding.UTF8.GetBytes(id.ToString()), cancellationToken: cancellationToken);
        }
    }

    private async Task SendEmailAsync(Guid emailId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryManagementDbContext>();
        EmailOutbox email;
        using (var transaction = await context.BeginTransactionAsync(cancellationToken))
        {
            var existing = await context.EmailOutboxes.IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == emailId && !x.DeletedOn.HasValue, cancellationToken);
            if (existing == null || existing.Status != EmailOutboxStatus.Pending
                || existing.NextAttemptOn > DateTime.UtcNow)
            {
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            email = existing;
            email.StartSending(DateTime.UtcNow);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(60));
        try
        {
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            await emailService.SendAsync(email.To, email.Subject, email.Body, timeout.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Keep Sending: delivery is uncertain and requires review after restart.
            throw;
        }
        catch (Exception exception)
        {
            // Explicit SMTP 4xx rejection is safe to retry. A dropped connection or
            // timeout may occur after SMTP accepted the message: avoid blind resend.
            var transient = exception is EmailConnectionException || (exception is SmtpCommandException smtp
                && (int)smtp.StatusCode >= 400 && (int)smtp.StatusCode < 500);
            DateTime? nextAttempt = transient && email.Attempts < 3
                ? DateTime.UtcNow.AddSeconds(30 * Math.Pow(2, email.Attempts - 1)) : null;
            email.MarkFailed($"{exception.GetType().Name}: SMTP delivery failed; review server logs or delivery before resending.", nextAttempt);
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("Email {EmailId} failed ({ErrorType}); attempt {Attempts}, status {Status}.",
                email.Id, exception.GetType().Name, email.Attempts, email.Status);
            return;
        }

        // Keep persistence outside the SMTP catch: a DB failure after a successful
        // send must never be mistaken for a safe-to-retry SMTP failure.
        email.MarkSent(DateTime.UtcNow);
        await context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Email {EmailId} accepted by SMTP.", email.Id);
    }
}
