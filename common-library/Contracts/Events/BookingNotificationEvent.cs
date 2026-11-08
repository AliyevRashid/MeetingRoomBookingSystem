namespace common_library.Contracts.Events;

public  record BookingNotificationEvent(
    Guid BookingId,
    string RoomName,
    string AuthorEmail,
    string Subject,
    List<string> RecipientsEmails,
    DateTime TriggeredAt);
