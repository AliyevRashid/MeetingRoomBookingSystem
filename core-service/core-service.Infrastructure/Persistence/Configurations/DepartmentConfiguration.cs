using core_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace core_service.Infrastructure.Persistence.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id).UseIdentityByDefaultColumn(); 

        builder.Property(d => d.Name)
               .IsRequired()
               .HasMaxLength(64);

        builder.Metadata
               .FindNavigation(nameof(Department.Users))!
               .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
