using common_library;
using core_service.Application.Interfaces;
using core_service.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace core_service.Application.Bookings.Commands.RescheduleBooking;

public record RescheduleBookingCommand(Guid BookingId, DateTime NewStartTime, DateTime NewEndTime) : IRequest<Result>;

public class RescheduleBookingCommandHandler : IRequestHandler<RescheduleBookingCommand, Result>
{
    private readonly IBookingAvailabilityChecker _bookingRepository;
    private readonly ICoreServiceDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBookingNotificationPublisher _notificationPublisher;

    public RescheduleBookingCommandHandler(IBookingAvailabilityChecker bookingRepository,
                                           ICoreServiceDbContext context,
                                           ICurrentUserService currentUserService,
                                           IBookingNotificationPublisher notificationPublisher)
    {
        _bookingRepository = bookingRepository;
        _context = context;
        _currentUserService = currentUserService;
        _notificationPublisher = notificationPublisher;
    }
    public async Task<Result> Handle(RescheduleBookingCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue || !_currentUserService.OrganizationId.HasValue)
        {
            return Result.Failure(ErrorCodes.Unauthorized);
        }
        var organizationId = _currentUserService.OrganizationId.Value;
        var userId = _currentUserService.UserId.Value;
        var booking = await _context.Bookings
                                    .Include(b => b.Room)
                                    .Include(b=>b.Author)
                                    .Where(b => b.Id == request.BookingId
                                             && b.Room.OrganizationId == organizationId)
                                    .FirstOrDefaultAsync(cancellationToken);
        if (booking is null)
        {
            return Result.Failure(ErrorCodes.NotFound);
        }
        var isAdmin = _currentUserService.Role == UserRoles.Admin;
        if (booking.AuthorId != userId && !isAdmin)
        {
            return Result.Failure(ErrorCodes.Unauthorized);
        }
        var rescheduleResult = booking.Reschedule(request.NewStartTime, request.NewEndTime);
        if (rescheduleResult.IsFailure)
        {
            return rescheduleResult;
        }

        var hasOverlap = await _bookingRepository.HasOverlapAsync(booking.RoomId,
                                                                  request.NewStartTime,
                                                                  request.NewEndTime,
                                                                  booking.Id,
                                                                  cancellationToken:cancellationToken);

        if (hasOverlap)
        {
            return Result.Failure(ErrorCodes.Conflict);
        }

        booking.Room.UpdateDate();

        var participantIds = await _context.BookingParticipants
                                           .AsNoTracking()
                                           .Where(bp => bp.BookingId == booking.Id)
                                           .Select(bp => bp.UserId)
                                           .ToListAsync(cancellationToken);

        var recipientIds = participantIds.Append(booking.AuthorId);

        await _notificationPublisher.PublishNotificationAsync(
            booking,
            booking.Room.Name,
            booking.Author.Email,
            organizationId,
            recipientIds,
            $"{booking.Subject} rescheduled", 
            cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(ErrorCodes.ConcurrencyConflict);
        }
    }
}