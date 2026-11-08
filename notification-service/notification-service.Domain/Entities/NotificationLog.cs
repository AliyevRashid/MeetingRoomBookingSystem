using notification_service.Domain.Enums;

namespace notification_service.Domain.Entities;

public sealed class NotificationLog
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
}
