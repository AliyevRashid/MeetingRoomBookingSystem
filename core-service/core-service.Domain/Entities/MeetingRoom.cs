using common_library;
using System;
using System.Collections.Generic;
using System.Text;

namespace core_service.Domain.Entities;

public class MeetingRoom
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Capacity { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Organization Organization { get; private set; } = null!;
    public DateTime UpdatedAt { get; private set; }
    private readonly List<Booking> _bookings = new();
    public IReadOnlyCollection<Booking> Bookings => _bookings.AsReadOnly();

    private MeetingRoom() { } 

    public static Result<MeetingRoom> Create(string name, int capacity, Guid organizationId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<MeetingRoom>.Failure(ErrorCodes.WrongInputError);
        }

        if (capacity <= 0)
        {
            return Result<MeetingRoom>.Failure(ErrorCodes.WrongInputError);
        }

        if (organizationId == Guid.Empty)
        {
            return Result<MeetingRoom>.Failure(ErrorCodes.WrongInputError);
        }

        return Result<MeetingRoom>.Success(new MeetingRoom
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Capacity = capacity,
            OrganizationId = organizationId,
            UpdatedAt = DateTime.UtcNow
        });
    }

    public Result UpdateDetails(string newName, int newCapacity)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            return Result.Failure(ErrorCodes.WrongInputError);
        }

        if (newCapacity <= 0)
        {
            return Result.Failure(ErrorCodes.WrongInputError);
        }

        Name = newName.Trim();
        Capacity = newCapacity;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    public void UpdateDate()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
