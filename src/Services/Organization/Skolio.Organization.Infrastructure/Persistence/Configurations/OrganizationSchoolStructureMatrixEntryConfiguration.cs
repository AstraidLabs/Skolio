using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence.Configurations;

public sealed class OrganizationSchoolStructureMatrixEntryConfiguration : IEntityTypeConfiguration<OrganizationSchoolStructureMatrixEntry>
{
    public void Configure(EntityTypeBuilder<OrganizationSchoolStructureMatrixEntry> builder)
    {
        builder.ToTable("organization_school_structure_matrix_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ParentScopeMatrixId).HasColumnName("parent_scope_matrix_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.TranslationKey).HasColumnName("translation_key").HasMaxLength(128).IsRequired();
        builder.Property(x => x.UsesGradeLevels).HasColumnName("uses_grade_levels").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.UsesClasses).HasColumnName("uses_classes").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.UsesGroups).HasColumnName("uses_groups").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.GroupIsPrimaryStructure).HasColumnName("group_is_primary_structure").HasDefaultValue(false).IsRequired();

        builder.HasOne(x => x.ParentScopeMatrix)
            .WithMany()
            .HasForeignKey(x => x.ParentScopeMatrixId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ParentScopeMatrixId).IsUnique();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}
