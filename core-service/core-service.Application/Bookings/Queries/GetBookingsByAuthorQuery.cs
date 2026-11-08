using common_library;
using core_service.Application.Interfaces;
using core_service.Application.Models;
using core_service.Application.ViewModels;
using Dapper;
using MediatR;

namespace core_service.Application.Bookings.Queries;

public record GetBookingsByAuthorQuery : PagedRequest, IRequest<Result<PagedResult<BookingItemVM>>>
{
    public GetBookingsByAuthorQuery() { }

    public GetBookingsByAuthorQuery(int pageNumber = 1, int pageSize = 10)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}

public class GetBookingsByAuthorHandler : IRequestHandler<GetBookingsByAuthorQuery, Result<PagedResult<BookingItemVM>>>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrentUserService _currentUserService;

    public GetBookingsByAuthorHandler(IDbConnectionFactory connectionFactory, ICurrentUserService currentUserService)
    {
        _connectionFactory = connectionFactory;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PagedResult<BookingItemVM>>> Handle(GetBookingsByAuthorQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            return Result<PagedResult<BookingItemVM>>.Failure(ErrorCodes.Unauthorized);
        }

        var authorId = _currentUserService.UserId.Value;

        var parameters = new DynamicParameters();
        parameters.Add("AuthorId", authorId);
        parameters.Add("PageSize", request.PageSize);
        parameters.Add("Offset", request.Offset);

        string multiSql = $@"
            SELECT COUNT(1) 
            FROM ""Bookings"" b 
            WHERE b.""AuthorId"" = @AuthorId;

            SELECT 
                b.""Id"", 
                b.""RoomId"", 
                r.""Name"" AS RoomName, 
                b.""StartTime"", 
                b.""EndTime"", 
                b.""CreatedAt"", 
                b.""Subject""
            FROM ""Bookings"" b
            LEFT JOIN ""MeetingRooms"" r ON b.""RoomId"" = r.""Id""
            WHERE b.""AuthorId"" = @AuthorId
            ORDER BY b.""StartTime"" DESC
            LIMIT @PageSize OFFSET @Offset;";

        using var connection = _connectionFactory.CreateConnection();
        using var multi = await connection.QueryMultipleAsync(multiSql, parameters);

        var totalCount = await multi.ReadSingleAsync<int>();
        var items = (await multi.ReadAsync<BookingItemVM>()).AsList();

        var pagedResult = new PagedResult<BookingItemVM>(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount
        );

        return Result<PagedResult<BookingItemVM>>.Success(pagedResult);
    }
}
