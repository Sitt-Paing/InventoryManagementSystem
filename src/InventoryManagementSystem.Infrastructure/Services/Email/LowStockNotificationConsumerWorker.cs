using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace InventoryManagementSystem.Infrastructure.Services.Email;

public sealed class LowStockNotificationConsumerWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqEmailSettings> rabbitMqOptions,
    IConfiguration configuration,
    ILogger<LowStockNotificationConsumerWorker> logger) : BackgroundService
{
    private static readonly TimeSpan EmptyQueueDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue<bool>(
                "LowStockNotifications:ConsumerEnabled"))
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Low stock consumer failed. Retrying in 10 seconds.");
            }

            await Task.Delay(RetryDelay, stoppingToken);
        }
    }

    private async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        var settings = rabbitMqOptions.Value;
        var factory = new ConnectionFactory
        {
            HostName = settings.Host,
            Port = settings.Port,
            UserName = settings.UserName,
            Password = settings.Password,
            VirtualHost = settings.VirtualHost,
            AutomaticRecoveryEnabled = false,
            ClientProvidedName = "Inventory low stock consumer"
        };

        await using var connection =
            await factory.CreateConnectionAsync(cancellationToken);

        await using var channel =
            await connection.CreateChannelAsync(
                cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: LowStockOutBoxPublisher.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        while (!cancellationToken.IsCancellationRequested
            && connection.IsOpen && channel.IsOpen)
        {
            var message = await channel.BasicGetAsync(
                queue: LowStockOutBoxPublisher.QueueName,
                autoAck: false,
                cancellationToken: cancellationToken);

            if (message is null)
            {
                await Task.Delay(EmptyQueueDelay, cancellationToken);
                continue;
            }

            var body = Encoding.UTF8.GetString(message.Body.Span);

            if (!Guid.TryParseExact(body, "D", out var outboxId)
                || outboxId == Guid.Empty)
            {
                logger.LogWarning("Rejected an invalid low stock message.");

                await channel.BasicRejectAsync(
                    message.DeliveryTag,
                    requeue: false,
                    cancellationToken: cancellationToken);

                continue;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var writer = scope.ServiceProvider
                .GetRequiredService<LowStockNotificationWriter>();

            bool notificationExists = await writer.CreateAsync(
                outboxId, cancellationToken);

            if (!notificationExists)
            {
                logger.LogWarning(
                    "Rejected low stock message {OutboxId}. Outbox row was not found.",
                    outboxId);

                await channel.BasicRejectAsync(
                    message.DeliveryTag,
                    requeue: false,
                    cancellationToken: cancellationToken);

                continue;
            }

            await channel.BasicAckAsync(
                message.DeliveryTag,
                multiple: false,
                cancellationToken: cancellationToken);
        }
    }
}