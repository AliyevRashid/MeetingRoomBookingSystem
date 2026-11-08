using core_service.Application.Interfaces;
using core_service.Domain.Entities;
using core_service.Infrastructure.Persistence.Configurations;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace core_service.Infrastructure;

public class CoreServiceDbContext : DbContext, ICoreServiceDbContext
{
    public CoreServiceDbContext(DbContextOptions<CoreServiceDbContext> options) : base(options) { }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<User> Users => Set<User>();
    public DbSet<MeetingRoom> MeetingRooms => Set<MeetingRoom>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingParticipant> BookingParticipants => Set<BookingParticipant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingConfiguration).Assembly);

        modelBuilder.AddTransactionalOutboxEntities();
    }
}
