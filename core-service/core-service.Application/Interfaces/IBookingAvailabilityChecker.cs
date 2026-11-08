using core_service.Domain.Entities;

namespace core_service.Application.Interfaces;

public interface IBookingAvailabilityChecker
{
    Task<bool> HasOverlapAsync(Guid roomId, DateTime startTime, DateTime endTime, Guid? excludeBookingId = null, CancellationToken cancellationToken = default);
}
