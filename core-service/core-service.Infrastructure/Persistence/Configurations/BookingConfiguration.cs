using core_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace core_service.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings");

        builder.HasKey(b => b.Id);

        builder.Property<uint>("xmin")
               .HasColumnType("xmin")
               .IsRowVersion();

        builder.Property(b => b.StartTime)
               .IsRequired();

        builder.Property(b => b.EndTime)
               .IsRequired();

        builder.Property(b => b.CreatedAt)
               .IsRequired();

        builder.HasOne(b => b.Room)
               .WithMany(r => r.Bookings)
               .HasForeignKey(b => b.RoomId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Author)
               .WithMany(u => u.Bookings)
               .HasForeignKey(b => b.AuthorId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
