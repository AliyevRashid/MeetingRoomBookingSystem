namespace core_service.Application.Interfaces;

public interface IBookingNotificationService
{
    Task NotifyRoomStatusChangedAsync(
        Guid organizationId,
        Guid roomId,
        string status,
        Guid? activeBookingId,
        CancellationToken cancellationToken = default);
}
