using Bogus;
using core_service.Domain.Entities;
using core_service.Domain.Enums;

namespace core_service.UnitTests.TestDataBuilders;

public class UserBuilder
{
    private string _email = new Faker().Internet.Email();
    private string _fullName = new Faker().Name.FullName();
    private Guid _organizationId = Guid.NewGuid();
    private int _departmentId = 1;
    private UserRoles _role = UserRoles.Default;

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UserBuilder WithFullName(string fullName)
    {
        _fullName = fullName;
        return this;
    }

    public UserBuilder WithOrganizationId(Guid organizationId)
    {
        _organizationId = organizationId;
        return this;
    }

    public UserBuilder WithDepartmentId(int departmentId)
    {
        _departmentId = departmentId;
        return this;
    }

    public UserBuilder WithRole(UserRoles role)
    {
        _role = role;
        return this;
    }

    public User Build()
    {
        var result = User.Create(_email, _fullName, _organizationId, _departmentId);
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Failed to build User: {result.ErrorCode}");
        }

        var user = result.Value;

        if (_role != UserRoles.Default)
        {
            typeof(User).GetProperty(nameof(User.Role))?.SetValue(user, _role);
        }

        return user;
    }
}