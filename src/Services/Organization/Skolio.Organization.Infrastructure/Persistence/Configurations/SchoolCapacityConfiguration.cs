using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence.Configurations;

public sealed class SchoolCapacityConfiguration : IEntityTypeConfiguration<SchoolCapacity>
{
    public void Configure(EntityTypeBuilder<SchoolCapacity> builder)
    {
        builder.ToTable("school_capacities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(x => x.CapacityType).HasColumnName("capacity_type").HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(x => x.MaxCapacity).HasColumnName("max_capacity").IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);

        builder.HasOne(x => x.School)
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SchoolId);
        builder.HasIndex(x => new { x.SchoolId, x.CapacityType }).IsUnique();
    }
}
