using core_service.Application.Interfaces;
using core_service.Domain.Entities;
using core_service.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace core_service.Infrastructure.Persistence.Repositories;

public class BookingAvailabilityChecker: IBookingAvailabilityChecker
{
    private readonly CoreServiceDbContext _context;

    public BookingAvailabilityChecker(CoreServiceDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasOverlapAsync(Guid roomId, DateTime startTime, DateTime endTime, Guid? excludeBookingId = null, CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .AnyAsync(b => b.RoomId == roomId
                        && (excludeBookingId == null || b.Id != excludeBookingId)
                        && b.StartTime < endTime
                        && b.EndTime > startTime, cancellationToken);
    }

}
