using InventoryManagementSystem.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;

namespace InventoryManagementSystem.Infrastructure.Services.Email;

public class  SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<SmtpEmailService> _logger;
    public SmtpEmailService(IOptions<SmtpSettings> options, ILogger<SmtpEmailService> logger)
    {
        _smtpSettings = options.Value;
        _logger = logger;
    }


    public async Task SendAsync (string to,string subject, string body, CancellationToken cancellationToken)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;

        message.Body = new TextPart("plain")
        {
            Text = body
        };

        using var client = new SmtpClient();
        var security = _smtpSettings.UserStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None;

        try
        {
            await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, security, cancellationToken);
        }
        catch (Exception exception) when (exception is System.Net.Sockets.SocketException || exception is IOException)
        {
            throw new EmailConnectionException(exception);
        }
        if (!string.IsNullOrWhiteSpace(_smtpSettings.UserName))
        {
            await client.AuthenticateAsync(
                _smtpSettings.UserName,
                _smtpSettings.Password,
                cancellationToken);
        }
        await client.SendAsync(message, cancellationToken);
        try
        {
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception exception)
        {
            // SendAsync already returned SMTP acceptance. Disconnect failure does
            // not change that result and must not trigger another delivery.
            _logger.LogWarning("SMTP disconnect failed after message acceptance ({ErrorType}).", exception.GetType().Name);
        }
    }
}
