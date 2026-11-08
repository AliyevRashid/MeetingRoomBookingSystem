namespace core_service.Application.ViewModels;

public record RoomBookingItemVM(Guid Id,
                                Guid AuthorId,
                                string AuthorName,
                                DateTime StartTime,
                                DateTime EndTime,
                                DateTime CreatedAt);

