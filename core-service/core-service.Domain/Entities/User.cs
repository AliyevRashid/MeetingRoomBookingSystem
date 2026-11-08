using common_library;
using core_service.Domain.Enums;

namespace core_service.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public UserRoles Role { get; private set; } = UserRoles.Default;
    public Guid OrganizationId { get; private set; }
    public Organization Organization { get; private set; } = null!;

    public int DepartmentId { get; private set; }
    public Department Department { get; private set; } = null!;

    private readonly List<Booking> _bookings = new();
    public IReadOnlyCollection<Booking> Bookings => _bookings.AsReadOnly();

    private User() { }

    public static Result<User> Create(string email, string fullName, Guid organizationId, int departmentId)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return Result<User>.Failure(ErrorCodes.WrongInputError);
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return Result<User>.Failure(ErrorCodes.WrongInputError);
        }

        if (organizationId == Guid.Empty)
        {
            return Result<User>.Failure(ErrorCodes.WrongInputError);
        }

        if (departmentId <= 0)
        {
            return Result<User>.Failure(ErrorCodes.WrongInputError);
        }

        return Result<User>.Success(new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            FullName = fullName.Trim(),
            OrganizationId = organizationId,
            DepartmentId = departmentId,
            Role = UserRoles.Default
        });
    }
}
