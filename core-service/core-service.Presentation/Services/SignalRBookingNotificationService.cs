using core_service.Application.Interfaces;
using core_service.Presentation.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace core_service.Presentation.Services;

public class SignalRBookingNotificationService : IBookingNotificationService
{
    private readonly IHubContext<BookingHub> _hubContext;

    public SignalRBookingNotificationService(IHubContext<BookingHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyRoomStatusChangedAsync(
        Guid organizationId,
        Guid roomId,
        string status,
        Guid? activeBookingId,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients
            .Group($"org_{organizationId}")
            .SendAsync("RoomStatusChanged", new
            {
                RoomId = roomId,
                Status = status,
                ActiveBookingId = activeBookingId,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);
    }
}