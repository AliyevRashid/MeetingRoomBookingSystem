using core_service.Domain.Entities;

namespace core_service.Application.Interfaces;

public interface IBookingNotificationPublisher
{
    Task PublishNotificationAsync(Booking booking,
                                  string roomName,
                                  string authorEmail,
                                  Guid organizationId,
                                  IEnumerable<Guid> recipientUserIds,
                                  string subject,
                                  CancellationToken cancellationToken = default);
}
