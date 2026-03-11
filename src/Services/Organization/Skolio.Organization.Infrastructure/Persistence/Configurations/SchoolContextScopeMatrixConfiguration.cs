using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence.Configurations;

public sealed class SchoolContextScopeMatrixConfiguration : IEntityTypeConfiguration<SchoolContextScopeMatrix>
{
    public void Configure(EntityTypeBuilder<SchoolContextScopeMatrix> builder)
    {
        builder.ToTable("school_context_scope_matrices");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SchoolType).HasColumnName("school_type_code").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.TranslationKey).HasColumnName("translation_key").HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);

        builder.HasIndex(x => x.SchoolType).IsUnique();
        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasMany(x => x.Capabilities)
            .WithOne(x => x.Matrix)
            .HasForeignKey(x => x.MatrixId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.AllowedRoles)
            .WithOne(x => x.Matrix)
            .HasForeignKey(x => x.MatrixId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.AllowedProfileSections)
            .WithOne(x => x.Matrix)
            .HasForeignKey(x => x.MatrixId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.AllowedCreateUserFlows)
            .WithOne(x => x.Matrix)
            .HasForeignKey(x => x.MatrixId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.AllowedUserManagementFlows)
            .WithOne(x => x.Matrix)
            .HasForeignKey(x => x.MatrixId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.AllowedOrganizationSections)
            .WithOne(x => x.Matrix)
            .HasForeignKey(x => x.MatrixId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.AllowedAcademicsSections)
            .WithOne(x => x.Matrix)
            .HasForeignKey(x => x.MatrixId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
