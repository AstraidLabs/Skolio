using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence.Configurations;

public sealed class OrganizationRegistryMatrixEntryConfiguration : IEntityTypeConfiguration<OrganizationRegistryMatrixEntry>
{
    public void Configure(EntityTypeBuilder<OrganizationRegistryMatrixEntry> builder)
    {
        builder.ToTable("organization_registry_matrix_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ParentScopeMatrixId).HasColumnName("parent_scope_matrix_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.TranslationKey).HasColumnName("translation_key").HasMaxLength(128).IsRequired();
        builder.Property(x => x.RequiresIzo).HasColumnName("requires_izo").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.RequiresRedIzo).HasColumnName("requires_red_izo").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.RequiresIco).HasColumnName("requires_ico").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.RequiresDataBox).HasColumnName("requires_data_box").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.RequiresFounder).HasColumnName("requires_founder").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.RequiresTeachingLanguage).HasColumnName("requires_teaching_language").HasDefaultValue(false).IsRequired();

        builder.HasOne(x => x.ParentScopeMatrix)
            .WithMany()
            .HasForeignKey(x => x.ParentScopeMatrixId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ParentScopeMatrixId).IsUnique();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}
