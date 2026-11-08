using common_library;
using core_service.Application.Interfaces;
using core_service.Application.ViewModels;
using core_service.Domain.Entities;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace core_service.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly CoreServiceDbContext _context;
    private readonly TokenService _tokenService;

    public AuthService(CoreServiceDbContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResultVm>> HandleGoogleLoginAsync(string email, string fullName)
    {
        var normalizedEmail = email.ToLowerInvariant();

        var user = await _context.Users
            .Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user != null)
        {
            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, user.OrganizationId, user.Role);
            return Result<AuthResultVm>.Success(new AuthResultVm(accessToken, IsRegistered: true));
        }
        else
        {
            var regToken = _tokenService.GenerateRegistrationToken(email, fullName);
            return Result<AuthResultVm>.Success(new AuthResultVm(regToken, IsRegistered: false));
        }
    }

    public async Task<Result<string>> CompleteRegistrationAsync(string regToken, string taxId, int departmentId)
    {
        var principal = _tokenService.ValidateRegistrationToken(regToken);
        if (principal == null)
        {
            return Result<string>.Failure(ErrorCodes.Unauthorized);
        }

        var email = principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var fullName = principal.FindFirst("fullName")?.Value;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(fullName))
        {
            return Result<string>.Failure(ErrorCodes.WrongInputError);
        }

        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.TaxId == taxId);

        if (organization == null)
        {
            return Result<string>.Failure(ErrorCodes.NotFound);
        }

        var userResult = User.Create(email, fullName, organization.Id, departmentId);
        if (userResult.IsFailure)
        {
            return Result<string>.Failure(userResult.ErrorCode);
        }

        _context.Users.Add(userResult.Value);
        await _context.SaveChangesAsync();

        var accessToken = _tokenService.GenerateAccessToken(userResult.Value.Id, userResult.Value.Email, organization.Id, userResult.Value.Role);
        return Result<string>.Success(accessToken);
    }


    public async Task<Result<IEnumerable<DepartmentVM>>> GetDepartmentsByTaxIdAsync(string taxId, CancellationToken cancellationToken)
    {
        var departments = await _context.Departments
            .AsNoTracking()
            .Where(d => d.Organization.TaxId == taxId)
            .Select(d => new DepartmentVM(d.Id, d.Name))
            .ToListAsync(cancellationToken);

        if (departments == null || !departments.Any())
        {
            return Result<IEnumerable<DepartmentVM>>.Failure(ErrorCodes.NotFound);
        }

        return Result<IEnumerable<DepartmentVM>>.Success(departments);
    }
}
