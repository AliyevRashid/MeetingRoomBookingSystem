using common_library.Contracts.Events;
using core_service.Application.Interfaces;
using core_service.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace core_service.Infrastructure.Services;

public class BookingNotificationPublisher : IBookingNotificationPublisher
{
    private readonly ICoreServiceDbContext _context;
    private readonly IPublishEndpoint _publishEndpoint;

    public BookingNotificationPublisher(ICoreServiceDbContext context, IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishNotificationAsync(
        Booking booking,
        string roomName,
        string authorEmail,
        Guid organizationId,
        IEnumerable<Guid> recipientUserIds,
        string subject,
        CancellationToken cancellationToken = default)
        
    {
        var distinctUserIds = recipientUserIds.Distinct().ToList();

        if (distinctUserIds.Count == 0)
        {
            return;
        }

        var recipientEmails = await _context.Users
                                            .AsNoTracking()
                                            .Where(u => u.OrganizationId == organizationId 
                                                     && distinctUserIds.Contains(u.Id))
                                            .Select(u => u.Email)
                                            .ToListAsync(cancellationToken);

        var notificationEvent = new BookingNotificationEvent(booking.Id,
                                                             roomName,
                                                             authorEmail,
                                                             subject,
                                                             recipientEmails,
                                                             DateTime.UtcNow);

        await _publishEndpoint.Publish(notificationEvent, cancellationToken);
    }
}