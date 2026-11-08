namespace core_service.Application.ViewModels;

public record MeetingRoomVM(Guid Id,
                            string Name,
                            int Capacity,
                            Guid OrganizationId);

