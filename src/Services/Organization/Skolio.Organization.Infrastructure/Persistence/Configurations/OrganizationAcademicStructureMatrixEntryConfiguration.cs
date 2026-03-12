using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence.Configurations;

public sealed class OrganizationAcademicStructureMatrixEntryConfiguration : IEntityTypeConfiguration<OrganizationAcademicStructureMatrixEntry>
{
    public void Configure(EntityTypeBuilder<OrganizationAcademicStructureMatrixEntry> builder)
    {
        builder.ToTable("organization_academic_structure_matrix_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ParentScopeMatrixId).HasColumnName("parent_scope_matrix_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.TranslationKey).HasColumnName("translation_key").HasMaxLength(128).IsRequired();
        builder.Property(x => x.UsesSubjects).HasColumnName("uses_subjects").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.UsesFieldOfStudy).HasColumnName("uses_field_of_study").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.SubjectIsClassBound).HasColumnName("subject_is_class_bound").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.FieldOfStudyIsRequired).HasColumnName("field_of_study_is_required").HasDefaultValue(false).IsRequired();

        builder.HasOne(x => x.ParentScopeMatrix)
            .WithMany()
            .HasForeignKey(x => x.ParentScopeMatrixId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ParentScopeMatrixId).IsUnique();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}
