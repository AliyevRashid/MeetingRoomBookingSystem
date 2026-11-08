using core_service.Application.Models;

namespace core_service.Application.ViewModels;

public record RoomDetailsVM(Guid Id,
                             string Name,
                             int Capacity,
                             Guid OrganizationId,
                             PagedResult<RoomBookingItemVM> Bookings);
