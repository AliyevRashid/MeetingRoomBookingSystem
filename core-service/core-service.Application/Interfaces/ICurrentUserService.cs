using core_service.Domain.Enums;

namespace core_service.Application.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    UserRoles? Role { get; }
    Guid? OrganizationId { get; }
    Guid? DepartmentId { get; }
    bool IsAuthenticated { get; }
}
