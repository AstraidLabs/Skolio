using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence.Configurations;

public sealed class OrganizationCapacityMatrixEntryConfiguration : IEntityTypeConfiguration<OrganizationCapacityMatrixEntry>
{
    public void Configure(EntityTypeBuilder<OrganizationCapacityMatrixEntry> builder)
    {
        builder.ToTable("organization_capacity_matrix_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ParentScopeMatrixId).HasColumnName("parent_scope_matrix_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.TranslationKey).HasColumnName("translation_key").HasMaxLength(128).IsRequired();
        builder.Property(x => x.CapacityType).HasColumnName("capacity_type").HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(x => x.IsRequired).HasColumnName("is_required").HasDefaultValue(false).IsRequired();

        builder.HasOne(x => x.ParentScopeMatrix)
            .WithMany()
            .HasForeignKey(x => x.ParentScopeMatrixId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ParentScopeMatrixId, x.CapacityType }).IsUnique();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}
