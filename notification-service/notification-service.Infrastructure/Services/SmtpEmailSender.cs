using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using notification_service.Application.Services;
using notification_service.Infrastructure.Options;

namespace notification_service.Infrastructure.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _smtpOptions;
    public SmtpEmailSender(IOptions<SmtpOptions> smtpOptions)
    {
        _smtpOptions = smtpOptions.Value;
    }
    public async Task SendEmailAsync(List<string> recipientEmails, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (recipientEmails == null || !recipientEmails.Any())
        {
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtpOptions.SenderName, _smtpOptions.SenderEmail));

        foreach (var email in recipientEmails)
        {
            message.To.Add(MailboxAddress.Parse(email));
        }

        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = body
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            Console.WriteLine($"Connecting to SMTP server: {_smtpOptions.Server}\n{_smtpOptions.Port}\n{_smtpOptions.SenderEmail}\n{_smtpOptions.Password}");
            await client.ConnectAsync(_smtpOptions.Server, _smtpOptions.Port, SecureSocketOptions.StartTls, cancellationToken);

            await client.AuthenticateAsync(_smtpOptions.SenderEmail, _smtpOptions.Password, cancellationToken);

            await client.SendAsync(message, cancellationToken);

        }
        catch (Exception)
        {
            throw; 
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
        }
    }
}

