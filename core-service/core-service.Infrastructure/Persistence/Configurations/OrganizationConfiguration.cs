using core_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace core_service.Infrastructure.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name)
               .IsRequired()
               .HasMaxLength(64);

        builder.Property(o => o.TaxId)
               .IsRequired()
               .HasMaxLength(8);

        builder.Property(o => o.TaxId)
            .IsRequired()
            .HasMaxLength(8);

        builder.HasIndex(o => o.TaxId)
            .IsUnique();

        builder.Property(o => o.CreatedAt)
               .IsRequired();

        builder.Metadata
               .FindNavigation(nameof(Organization.Users))!
               .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Metadata
               .FindNavigation(nameof(Organization.Rooms))!
               .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
