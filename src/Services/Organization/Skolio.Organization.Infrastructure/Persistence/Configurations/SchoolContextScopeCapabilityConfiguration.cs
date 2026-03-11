using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence.Configurations;

public sealed class SchoolContextScopeCapabilityConfiguration : IEntityTypeConfiguration<SchoolContextScopeCapability>
{
    public void Configure(EntityTypeBuilder<SchoolContextScopeCapability> builder)
    {
        builder.ToTable("school_context_scope_capabilities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.MatrixId).HasColumnName("matrix_id").IsRequired();
        builder.Property(x => x.CapabilityCode).HasColumnName("capability_code").HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(x => x.TranslationKey).HasColumnName("translation_key").HasMaxLength(128).IsRequired();
        builder.Property(x => x.IsEnabled).HasColumnName("is_enabled").IsRequired();

        builder.HasIndex(x => new { x.MatrixId, x.CapabilityCode }).IsUnique();
    }
}
