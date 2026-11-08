using System.ComponentModel.DataAnnotations;

namespace notification_service.Infrastructure.Options;

public class SmtpOptions
{
    public const string SectionName = "SmtpOptions";

    [Required(ErrorMessage = "SMTP Server is required")]
    public string Server { get; set; } = "smtp.gmail.com";

    [Range(1, 65535, ErrorMessage = "Invalid SMTP Port")]
    public int Port { get; set; } = 587;

    [Required(ErrorMessage = "Sender Email is required")]
    [EmailAddress(ErrorMessage = "Invalid Sender Email format")]
    public string SenderEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sender Name is required")]
    public string SenderName { get; set; } = "Notification Service";

    [Required(ErrorMessage = "SMTP Password is required")]
    public string Password { get; set; } = string.Empty;
}
