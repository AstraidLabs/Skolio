using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence.Configurations;

public sealed class RoleDefinitionConfiguration : IEntityTypeConfiguration<RoleDefinition>
{
    public void Configure(EntityTypeBuilder<RoleDefinition> builder)
    {
        builder.ToTable("role_definitions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.RoleCode).HasColumnName("role_code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.TranslationKey).HasColumnName("translation_key").HasMaxLength(128).IsRequired();
        builder.Property(x => x.ScopeType).HasColumnName("scope_type").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.IsBootstrapAllowed).HasColumnName("is_bootstrap_allowed").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.IsCreateUserFlowAllowed).HasColumnName("is_create_user_flow_allowed").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.IsUserManagementAllowed).HasColumnName("is_user_management_allowed").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.SortOrder).HasColumnName("sort_order").HasDefaultValue(0).IsRequired();

        builder.HasIndex(x => x.RoleCode).IsUnique();
    }
}
