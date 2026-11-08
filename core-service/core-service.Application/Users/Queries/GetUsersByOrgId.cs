using common_library;
using core_service.Application.Interfaces;
using core_service.Application.Models;
using core_service.Application.ViewModels;
using Dapper;
using MediatR;

namespace core_service.Application.Users.Queries;

public record FilterUsersByNameQuery : PagedRequest, IRequest<Result<PagedResult<UserVM>>>
{
    public string? UserName { get; init; }

    public FilterUsersByNameQuery() { }
    public FilterUsersByNameQuery(int pageNumber = 1, int pageSize = 10, string? userName = null) 
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        UserName = userName;
    }
}
public class FilterUsersByNameQueryHandler : IRequestHandler<FilterUsersByNameQuery, Result<PagedResult<UserVM>>>
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrentUserService _currentUserService;
    public FilterUsersByNameQueryHandler(IDbConnectionFactory connectionFactory, ICurrentUserService currentUserService)
    {
        _connectionFactory = connectionFactory;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PagedResult<UserVM>>> Handle(FilterUsersByNameQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.OrganizationId.HasValue)
        {
            return Result<PagedResult<UserVM>>.Failure(ErrorCodes.Unauthorized);
        }

        var organizationId = _currentUserService.OrganizationId.Value;

        var whereClause = "WHERE u.\"OrganizationId\" = @OrganizationId";
        var parameters = new DynamicParameters();
        parameters.Add("OrganizationId", organizationId);
        parameters.Add("PageSize", request.PageSize);
        parameters.Add("Offset", request.Offset);

        if (!string.IsNullOrWhiteSpace(request.UserName) && request.UserName.Length >= 3)
        {
            whereClause += " AND u.\"FullName\" ILIKE @UserName";
            parameters.Add("UserName", $"%{request.UserName}%");
        }

        string countSql = $@"
            SELECT COUNT(1) 
            FROM ""Users"" u 
            {whereClause};";

        string dataSql = $@"
            SELECT 
                u.""Id"", 
                u.""FullName"" AS Name, 
                COALESCE(d.""Name"", 'No Department') AS DepartmentName
            FROM ""Users"" u
            LEFT JOIN ""Departments"" d ON u.""DepartmentId"" = d.""Id""
            {whereClause}
            ORDER BY u.""FullName"" ASC
            LIMIT @PageSize OFFSET @Offset;";

        using var connection = _connectionFactory.CreateConnection();

        var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);
        var users = (await connection.QueryAsync<UserVM>(dataSql, parameters)).AsList();

        var pagedResult = new PagedResult<UserVM>(
            users,
            request.PageNumber,
            request.PageSize,
            totalCount
        );

        return Result<PagedResult<UserVM>>.Success(pagedResult);
    }

}
