using Microsoft.AspNetCore.SignalR;

namespace core_service.Presentation.Hubs;

public class BookingHub : Hub
{
    public async Task JoinOrganizationGroup(Guid organizationId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"org_{organizationId}");
    }

    public async Task LeaveOrganizationGroup(Guid organizationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"org_{organizationId}");
    }
}
