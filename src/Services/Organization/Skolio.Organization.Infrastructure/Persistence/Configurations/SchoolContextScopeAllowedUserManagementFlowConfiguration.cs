using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence.Configurations;

public sealed class SchoolContextScopeAllowedUserManagementFlowConfiguration : IEntityTypeConfiguration<SchoolContextScopeAllowedUserManagementFlow>
{
    public void Configure(EntityTypeBuilder<SchoolContextScopeAllowedUserManagementFlow> builder)
    {
        builder.ToTable("school_context_scope_allowed_user_management_flows");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.MatrixId).HasColumnName("matrix_id").IsRequired();
        builder.Property(x => x.FlowCode).HasColumnName("flow_code").HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(x => x.TranslationKey).HasColumnName("translation_key").HasMaxLength(128).IsRequired();

        builder.HasIndex(x => new { x.MatrixId, x.FlowCode }).IsUnique();
    }
}
