using core_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace core_service.Infrastructure.Persistence.Configurations;

public class BookingParticipantConfiguration : IEntityTypeConfiguration<BookingParticipant>
{
    public void Configure(EntityTypeBuilder<BookingParticipant> builder)
    {
        builder.ToTable("BookingParticipants");

        builder.HasKey(bp => new { bp.BookingId, bp.UserId });

        builder.HasOne(bp => bp.Booking)
               .WithMany()
               .HasForeignKey(bp => bp.BookingId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
