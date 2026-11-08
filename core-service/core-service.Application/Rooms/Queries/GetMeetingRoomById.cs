using common_library;
using core_service.Application.Interfaces;
using core_service.Application.Models;
using core_service.Application.ViewModels;
using Dapper;
using MediatR;

namespace core_service.Application.Rooms.Queries;

public record GetMeetingRoomByIdQuery : PagedRequest, IRequest<Result<RoomDetailsVM>>
{
    public Guid RoomId { get; init; }
   

    public GetMeetingRoomByIdQuery() { }

    public GetMeetingRoomByIdQuery(
        Guid roomId,
        int pageNumber = 1,
        int pageSize = 5)
    {
        RoomId = roomId;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}

public class GetMeetingRoomByIdQueryHandler : IRequestHandler<GetMeetingRoomByIdQuery, Result<RoomDetailsVM>>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrentUserService _currentUserService;

    public GetMeetingRoomByIdQueryHandler(
        IDbConnectionFactory connectionFactory,
        ICurrentUserService currentUserService)
    {
        _connectionFactory = connectionFactory;
        _currentUserService = currentUserService;
    }

    public async Task<Result<RoomDetailsVM>> Handle(
        GetMeetingRoomByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.OrganizationId.HasValue)
        {
            return Result<RoomDetailsVM>.Failure(ErrorCodes.Unauthorized);
        }

        const string sql = """
            -- 1. Room info
            SELECT 
                r."Id", 
                r."Name", 
                r."Capacity", 
                r."OrganizationId"
            FROM "MeetingRooms" r
            WHERE r."Id" = @RoomId AND r."OrganizationId" = @OrganizationId;

            -- 2. Total bookings count
            SELECT COUNT(1)
            FROM "Bookings" b
            WHERE b."RoomId" = @RoomId;

            -- 3. Paginated bookings mapped directly to RoomBookingItemVM
            SELECT 
                b."Id", 
                b."AuthorId", 
                COALESCE(u."FullName", 'Unknown') AS AuthorName,
                b."StartTime", 
                b."EndTime", 
                b."CreatedAt"
            FROM "Bookings" b
            LEFT JOIN "Users" u ON b."AuthorId" = u."Id"
            WHERE b."RoomId" = @RoomId
            ORDER BY b."StartTime" DESC
            LIMIT @PageSize OFFSET @Offset;
            """;

        using var connection = _connectionFactory.CreateConnection();

        using var multi = await connection.QueryMultipleAsync(sql, new
        {
            request.RoomId,
            OrganizationId = _currentUserService.OrganizationId.Value,
            request.PageSize,
            request.Offset
        });

        var room = await multi.ReadFirstOrDefaultAsync<MeetingRoomVM>();
        if (room is null)
        {
            return Result<RoomDetailsVM>.Failure(ErrorCodes.NotFound);
        }

        var totalBookingsCount = await multi.ReadSingleAsync<int>();
        var bookingItems = (await multi.ReadAsync<RoomBookingItemVM>()).AsList();

        var pagedBookings = new PagedResult<RoomBookingItemVM>(
            bookingItems,
            request.PageNumber,
            request.PageSize,
            totalBookingsCount);

        return Result<RoomDetailsVM>.Success(new RoomDetailsVM(
            room.Id,
            room.Name,
            room.Capacity,
            room.OrganizationId,
            pagedBookings
        ));
    }
}
