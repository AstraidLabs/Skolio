using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence.Configurations;

public sealed class SchoolContextScopeAllowedOrganizationSectionConfiguration : IEntityTypeConfiguration<SchoolContextScopeAllowedOrganizationSection>
{
    public void Configure(EntityTypeBuilder<SchoolContextScopeAllowedOrganizationSection> builder)
    {
        builder.ToTable("school_context_scope_allowed_organization_sections");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.MatrixId).HasColumnName("matrix_id").IsRequired();
        builder.Property(x => x.SectionCode).HasColumnName("section_code").HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(x => x.TranslationKey).HasColumnName("translation_key").HasMaxLength(128).IsRequired();

        builder.HasIndex(x => new { x.MatrixId, x.SectionCode }).IsUnique();
    }
}
