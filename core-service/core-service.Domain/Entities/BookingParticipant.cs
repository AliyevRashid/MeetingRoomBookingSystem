namespace core_service.Domain.Entities;

public class BookingParticipant
{
    public Guid BookingId { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public Booking Booking { get; private set; } = null!;

    private BookingParticipant() { } 

    public BookingParticipant(Guid bookingId, Guid userId)
    {
        BookingId = bookingId;
        UserId = userId;
    }
}
