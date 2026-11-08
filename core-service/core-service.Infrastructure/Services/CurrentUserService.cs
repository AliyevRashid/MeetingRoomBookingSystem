using core_service.Application.Interfaces;
using core_service.Domain.Enums;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace core_service.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var sub = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email)
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue("email");

    public UserRoles? Role
    {
        get
        {
            var roleStr = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);
            return Enum.TryParse<UserRoles>(roleStr, true, out var role) ? role : null;
        }
    }

    public Guid? OrganizationId
    {
        get
        {
            var orgIdStr = _httpContextAccessor.HttpContext?.User?.FindFirstValue("organizationId");
            return Guid.TryParse(orgIdStr, out var id) ? id : null;
        }
    }

    public Guid? DepartmentId
    {
        get
        {
            var deptIdStr = _httpContextAccessor.HttpContext?.User?.FindFirstValue("departmentId");
            return Guid.TryParse(deptIdStr, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
