using core_service.Domain.Entities;

namespace core_service.UnitTests.TestBuilders;

public class BookingParticipantBuilder
{
    private Guid _bookingId = Guid.NewGuid();
    private Guid _userId = Guid.NewGuid();
    private User? _user;
    private Booking? _booking;

    public BookingParticipantBuilder WithBookingId(Guid bookingId)
    {
        _bookingId = bookingId;
        return this;
    }

    public BookingParticipantBuilder WithUserId(Guid userId)
    {
        _userId = userId;
        return this;
    }

    public BookingParticipantBuilder WithUser(User user)
    {
        _user = user;
        _userId = user.Id;
        return this;
    }

    public BookingParticipantBuilder WithBooking(Booking booking)
    {
        _booking = booking;
        _bookingId = booking.Id;
        return this;
    }

    public BookingParticipant Build()
    {
        var participant = new BookingParticipant(_bookingId, _userId);

        if (_user != null)
        {
            typeof(BookingParticipant).GetProperty(nameof(BookingParticipant.User))?.SetValue(participant, _user);
        }

        if (_booking != null)
        {
            typeof(BookingParticipant).GetProperty(nameof(BookingParticipant.Booking))?.SetValue(participant, _booking);
        }

        return participant;
    }
}