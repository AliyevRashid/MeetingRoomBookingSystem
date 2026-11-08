using core_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace core_service.Infrastructure.Persistence.Configurations;

public class MeetingRoomConfiguration : IEntityTypeConfiguration<MeetingRoom>
{
    public void Configure(EntityTypeBuilder<MeetingRoom> builder)
    {
        builder.ToTable("MeetingRooms");

        builder.HasKey(r => r.Id);

        builder.Property<uint>("xmin")
               .HasColumnType("xmin")
               .IsRowVersion();

        builder.Property(r => r.Name)
               .IsRequired()
               .HasMaxLength(64);

        builder.Property(r => r.Capacity)
               .IsRequired();

        builder.Property(r => r.UpdatedAt)
               .IsRequired();

        builder.HasOne(r => r.Organization)
               .WithMany(o => o.Rooms)
               .HasForeignKey(r => r.OrganizationId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
               .FindNavigation(nameof(MeetingRoom.Bookings))!
               .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
