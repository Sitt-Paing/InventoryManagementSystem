using InventoryManagementSystem.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace InventoryManagementSystem.Infrastructure.Services.Email;

public class  SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;
    public SmtpEmailService(IOptions<SmtpSettings> options)
    {
        _smtpSettings = options.Value;
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

        await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, security, cancellationToken);
        if (!string.IsNullOrWhiteSpace(_smtpSettings.UserName))
        {
            await client.AuthenticateAsync(
                _smtpSettings.UserName,
                _smtpSettings.Password,
                cancellationToken);
        }
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
