namespace core_service.Application.ViewModels;

public class BookingItemVM
{
    public Guid Id { get; init; }
    public Guid RoomId { get; init; }
    public string RoomName { get; init; } = string.Empty;
    public int Status { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public DateTime CreatedAt { get; init; }
    public string Subject { get; init; } = string.Empty;

    public BookingItemVM() { }
}
