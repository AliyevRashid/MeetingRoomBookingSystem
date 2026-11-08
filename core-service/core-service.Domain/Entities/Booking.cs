using common_library;
using core_service.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace core_service.Domain.Entities;

public class Booking
{
    public Guid Id { get; private set; }
    public Guid RoomId { get; private set; }
    public MeetingRoom Room { get; private set; } = null!;
    public Guid AuthorId { get; private set; }
    public User Author { get; private set; } = null!;
    [MaxLength(128)]
    public string Subject { get; private set; } = string.Empty;
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Booking() { }

    public static Result<Booking> Create(Guid roomId, Guid authorId, DateTime startTime, DateTime endTime, string subject)
    {
        var validationResult = ValidateTimeSlot(startTime, endTime);
        if (validationResult.IsFailure)
        {
            return Result<Booking>.Failure(validationResult.ErrorCode);
        }

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            AuthorId = authorId,
            StartTime = startTime,
            EndTime = endTime,
            CreatedAt = DateTime.UtcNow,
            Subject = subject?.Trim() ?? string.Empty
        };

        return Result<Booking>.Success(booking);
    }

    public Result Reschedule(DateTime newStartTime, DateTime newEndTime)
    {
        var validationResult = ValidateTimeSlot(newStartTime, newEndTime);
        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        StartTime = newStartTime;
        EndTime = newEndTime;
        UpdatedAt = DateTime.UtcNow;

        return Result.Success();
    }

    

    private static Result ValidateTimeSlot(DateTime start, DateTime end)
    {
        if (start >= end)
        {
            return Result.Failure(ErrorCodes.WrongInputError);
        }

        if ((end - start).TotalMinutes < 15)
        {
            return Result.Failure(ErrorCodes.WrongInputError);
        }

        return Result.Success();
    }
}
