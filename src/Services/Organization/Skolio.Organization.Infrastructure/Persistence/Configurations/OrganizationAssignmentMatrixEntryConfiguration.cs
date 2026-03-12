using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence.Configurations;

public sealed class OrganizationAssignmentMatrixEntryConfiguration : IEntityTypeConfiguration<OrganizationAssignmentMatrixEntry>
{
    public void Configure(EntityTypeBuilder<OrganizationAssignmentMatrixEntry> builder)
    {
        builder.ToTable("organization_assignment_matrix_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ParentScopeMatrixId).HasColumnName("parent_scope_matrix_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.TranslationKey).HasColumnName("translation_key").HasMaxLength(128).IsRequired();
        builder.Property(x => x.AllowsClassRoomAssignment).HasColumnName("allows_class_room_assignment").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.AllowsGroupAssignment).HasColumnName("allows_group_assignment").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.AllowsSubjectAssignment).HasColumnName("allows_subject_assignment").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.StudentRequiresClassPlacement).HasColumnName("student_requires_class_placement").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.StudentRequiresGroupPlacement).HasColumnName("student_requires_group_placement").HasDefaultValue(false).IsRequired();

        builder.HasOne(x => x.ParentScopeMatrix)
            .WithMany()
            .HasForeignKey(x => x.ParentScopeMatrixId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ParentScopeMatrixId).IsUnique();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}
