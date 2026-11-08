using Bogus;
using core_service.Domain.Entities;
using core_service.Domain.Enums;

namespace core_service.UnitTests.TestBuilders;

public class BookingBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _roomId = Guid.NewGuid();
    private Guid _authorId = Guid.NewGuid();
    private DateTime _startTime = DateTime.UtcNow.AddHours(1);
    private DateTime _endTime = DateTime.UtcNow.AddHours(2); 
    private string _subject = new Faker().Commerce.ProductName();

    private MeetingRoom? _room;
    private User? _author;

    public BookingBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }
    public BookingBuilder WithRoomId(Guid roomId)
    {
        _roomId = roomId;
        return this;
    }

    public BookingBuilder WithAuthorId(Guid authorId)
    {
        _authorId = authorId;
        return this;
    }

    public BookingBuilder WithTime(DateTime start, DateTime end)
    {
        _startTime = start;
        _endTime = end;
        return this;
    }

    public BookingBuilder WithSubject(string subject)
    {
        _subject = subject;
        return this;
    }

    public BookingBuilder WithRoom(MeetingRoom room)
    {
        _room = room;
        _roomId = room.Id;
        return this;
    }

    public BookingBuilder WithAuthor(User author)
    {
        _author = author;
        _authorId = author.Id;
        return this;
    }

    public Booking Build()
    {
        var result = Booking.Create(_roomId, _authorId, _startTime, _endTime, _subject);
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Failed to build Booking: {result.ErrorCode}");
        }

        var booking = result.Value;
        typeof(Booking).GetProperty(nameof(Booking.Id))?.SetValue(booking, _id);

        if (_room != null)
        {
            typeof(Booking).GetProperty(nameof(Booking.Room))?.SetValue(booking, _room);
        }

        if (_author != null)
        {
            typeof(Booking).GetProperty(nameof(Booking.Author))?.SetValue(booking, _author);
        }

        return booking;
    }
}