namespace core_service.Application.ViewModels;

public record BookingVM(Guid Id,
                        Guid RoomId,
                        string RoomName,
                        Guid AuthorId,
                        string Status,
                        DateTime StartTime,
                        DateTime EndTime,
                        DateTime CreatedAt);

