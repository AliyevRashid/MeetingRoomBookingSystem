using common_library;
using core_service.Application.ViewModels;

namespace core_service.Application.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResultVm>> HandleGoogleLoginAsync(string email, string fullName);
    Task<Result<string>> CompleteRegistrationAsync(string regToken, string taxId, int departmentId);
    Task<Result<IEnumerable<DepartmentVM>>> GetDepartmentsByTaxIdAsync(string taxId, CancellationToken cancellationToken);
}
