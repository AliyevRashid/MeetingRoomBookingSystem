using common_library;
using System;
using System.Collections.Generic;
using System.Text;

namespace core_service.Domain.Entities;

public class Organization
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string TaxId { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private readonly List<Department> _departments = new();
    public IReadOnlyCollection<Department> Departments => _departments.AsReadOnly();

    private readonly List<User> _users = new();
    public IReadOnlyCollection<User> Users => _users.AsReadOnly();

    private readonly List<MeetingRoom> _rooms = new();
    public IReadOnlyCollection<MeetingRoom> Rooms => _rooms.AsReadOnly();

    private Organization() { }

    public static Result<Organization> Create(string name, string taxId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<Organization>.Failure(ErrorCodes.WrongInputError);
        }

        if (!IsValidTaxId(taxId))
        {
            return Result<Organization>.Failure(ErrorCodes.WrongInputError);
        }

        return Result<Organization>.Success(new Organization
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            TaxId = taxId,
            CreatedAt = DateTime.UtcNow
        });
    }
    public Result UpdateTaxId(string taxId)
    {
        if (!IsValidTaxId(taxId))
        {
            return Result.Failure(ErrorCodes.WrongInputError);
        }

        TaxId = taxId;
        return Result.Success();
    }

    private static bool IsValidTaxId(string taxId)
    {
        return !string.IsNullOrWhiteSpace(taxId) && taxId.Length == 8 && taxId.All(char.IsDigit);
    }
}
