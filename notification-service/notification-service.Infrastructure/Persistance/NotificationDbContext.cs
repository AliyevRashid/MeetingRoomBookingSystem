using MassTransit;
using Microsoft.EntityFrameworkCore;
using notification_service.Domain.Entities;

namespace notification_service.Infrastructure.Persistance;

public sealed class NotificationDbContext : DbContext
{
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();

    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
