using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence.Configurations;

public sealed class SchoolScopeOverrideConfiguration : IEntityTypeConfiguration<SchoolScopeOverride>
{
    public void Configure(EntityTypeBuilder<SchoolScopeOverride> builder)
    {
        builder.ToTable("school_scope_overrides");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(x => x.MatrixId).HasColumnName("matrix_id").IsRequired();
        builder.Property(x => x.OverrideUsesClasses).HasColumnName("override_uses_classes");
        builder.Property(x => x.OverrideUsesGroups).HasColumnName("override_uses_groups");
        builder.Property(x => x.OverrideUsesSubjects).HasColumnName("override_uses_subjects");
        builder.Property(x => x.OverrideUsesFieldOfStudy).HasColumnName("override_uses_field_of_study");
        builder.Property(x => x.OverrideUsesDailyReports).HasColumnName("override_uses_daily_reports");
        builder.Property(x => x.OverrideUsesAttendance).HasColumnName("override_uses_attendance");
        builder.Property(x => x.OverrideUsesGrades).HasColumnName("override_uses_grades");
        builder.Property(x => x.OverrideUsesHomework).HasColumnName("override_uses_homework");

        builder.HasOne(x => x.School)
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Matrix)
            .WithMany()
            .HasForeignKey(x => x.MatrixId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SchoolId).IsUnique();
    }
}
