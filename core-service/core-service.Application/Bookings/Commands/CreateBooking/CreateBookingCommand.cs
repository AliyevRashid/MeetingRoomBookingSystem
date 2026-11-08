using common_library;
using core_service.Application.Interfaces;
using core_service.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace core_service.Application.Bookings.Commands.CreateBooking;

public record CreateBookingCommand(Guid RoomId,
                                   DateTime StartTime,
                                   DateTime EndTime,
                                   List<Guid> ParticipantIds,
                                   string Subject) : IRequest<Result<Guid>>;


public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, Result<Guid>>
{
    private readonly IBookingAvailabilityChecker _bookingRepository;
    private readonly ICoreServiceDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IBookingNotificationPublisher _notificationPublisher;

    public CreateBookingCommandHandler(
        IBookingAvailabilityChecker bookingRepository,
        ICoreServiceDbContext context,
        ICurrentUserService currentUserService,
        IBookingNotificationPublisher notificationPublisher)
    {
        _bookingRepository = bookingRepository;
        _context = context;
        _currentUserService = currentUserService;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result<Guid>> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue || !_currentUserService.OrganizationId.HasValue)
        {
            return Result<Guid>.Failure(ErrorCodes.Unauthorized);
        }

        var organizationId = _currentUserService.OrganizationId.Value;
        var userId = _currentUserService.UserId.Value;

        var room = await _context.MeetingRooms
                                 .Where(r => r.Id == request.RoomId && r.OrganizationId == organizationId)
                                 .FirstOrDefaultAsync(cancellationToken);

        if (room is null)
        {
            return Result<Guid>.Failure(ErrorCodes.NotFound);
        }

        var validParticipantsCount = await _context.Users
                                                   .CountAsync(u => request.ParticipantIds.Contains(u.Id)
                                                                 && u.OrganizationId == organizationId,
                                                                    cancellationToken);

        if (validParticipantsCount != request.ParticipantIds.Count)
        {
            return Result<Guid>.Failure(ErrorCodes.WrongInputError);
        }

        var authorEmail = await _context.Users
                                       .Where(u => u.Id == userId)
                                       .Select(u => u.Email)
                                       .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrEmpty(authorEmail))
        {
            return Result<Guid>.Failure(ErrorCodes.Unauthorized);
        }

        var bookingResult = Booking.Create(request.RoomId, userId, request.StartTime, request.EndTime, request.Subject);
        if (bookingResult.IsFailure)
        {
            return Result<Guid>.Failure(bookingResult.ErrorCode);
        }

        var booking = bookingResult.Value;

        var hasOverlap = await _bookingRepository.HasOverlapAsync(
                                                    request.RoomId,
                                                    request.StartTime,
                                                    request.EndTime,
                                                    cancellationToken: cancellationToken);

        if (hasOverlap)
        {
            return Result<Guid>.Failure(ErrorCodes.ConcurrencyConflict);
        }

        await _context.Bookings.AddAsync(booking, cancellationToken);
        room.UpdateDate();

        var distinctParticipantIds = request.ParticipantIds.Distinct().ToList();
        var participants = distinctParticipantIds.Select(pId => new BookingParticipant(booking.Id, pId));

        await _context.BookingParticipants.AddRangeAsync(participants, cancellationToken);

        await _notificationPublisher.PublishNotificationAsync(booking,
                                                              room.Name,
                                                              authorEmail,
                                                              organizationId,
                                                              distinctParticipantIds,
                                                              request.Subject, 
                                                              cancellationToken);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return Result<Guid>.Success(booking.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<Guid>.Failure(ErrorCodes.ConcurrencyConflict);
        }
    }
}