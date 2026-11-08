using core_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace core_service.Application.Interfaces;

public interface ICoreServiceDbContext
{
    DbSet<MeetingRoom> MeetingRooms { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<User> Users { get; }
    DbSet<Organization> Organizations { get; }
    DbSet<Department> Departments { get; }
    DbSet<BookingParticipant> BookingParticipants { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
