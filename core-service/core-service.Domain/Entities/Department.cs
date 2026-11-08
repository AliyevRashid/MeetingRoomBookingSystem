using common_library;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace core_service.Domain.Entities;

public class Department
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid OrganizationId { get; private set; }
    public Organization Organization { get; private set; } = null!;

    private readonly List<User> _users = new();
    public IReadOnlyCollection<User> Users => _users.AsReadOnly();

    private Department() { }

    public static Result<Department> Create(int id, string name, Guid organizationId)
    {
        if (id <= 0)
        {
            return Result<Department>.Failure(ErrorCodes.WrongInputError);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Department>.Failure(ErrorCodes.WrongInputError);
        }
        if (organizationId == Guid.Empty)
        {
            return Result<Department>.Failure(ErrorCodes.WrongInputError);
        }

        return Result<Department>.Success(new Department
        {
            Id = id,
            Name = name.Trim(),
            OrganizationId = organizationId
        });
    }

    public Result UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return Result.Failure(ErrorCodes.WrongInputError);
        }

        Name = newName.Trim();
        return Result.Success();
    }
}
