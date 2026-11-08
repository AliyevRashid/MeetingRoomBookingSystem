namespace notification_service.Application.Services;

public interface IEmailSender
{
    Task SendEmailAsync(
        List<string> recipientEmails,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}
