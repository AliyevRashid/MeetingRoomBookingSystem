using common_library;
using core_service.Application.Interfaces;
using core_service.Application.Models;
using core_service.Application.ViewModels;
using Dapper;
using MediatR;

namespace core_service.Application.Rooms.Queries;

public record GetMeetingRoomsQuery : PagedRequest, IRequest<Result<PagedResult<MeetingRoomVM>>>;

public class GetMeetingRoomsQueryHandler : IRequestHandler<GetMeetingRoomsQuery, Result<PagedResult<MeetingRoomVM>>>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrentUserService _currentUserService;

    public GetMeetingRoomsQueryHandler(
        IDbConnectionFactory connectionFactory,
        ICurrentUserService currentUserService)
    {
        _connectionFactory = connectionFactory;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PagedResult<MeetingRoomVM>>> Handle(
        GetMeetingRoomsQuery request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.OrganizationId.HasValue)
        {
            return Result<PagedResult<MeetingRoomVM>>.Failure(ErrorCodes.Unauthorized);
        }

        const string sql = """
            SELECT COUNT(1) 
            FROM "MeetingRooms" r
            WHERE r."OrganizationId" = @OrganizationId;

            SELECT 
                r."Id", 
                r."Name", 
                r."Capacity", 
                r."OrganizationId"
            FROM "MeetingRooms" r
            WHERE r."OrganizationId" = @OrganizationId
            ORDER BY r."Name" ASC
            LIMIT @PageSize OFFSET @Offset;
            """;

        using var connection = _connectionFactory.CreateConnection();

        using var multi = await connection.QueryMultipleAsync(sql, new
        {
            OrganizationId = _currentUserService.OrganizationId.Value,
            request.PageSize,
            request.Offset
        });

        var totalCount = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<MeetingRoomVM>()).AsList();

        return Result<PagedResult<MeetingRoomVM>>.Success(
            new PagedResult<MeetingRoomVM>(items, request.PageNumber, request.PageSize, totalCount));
    }
}
