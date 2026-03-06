using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Academics.Domain.Entities;

namespace Skolio.Academics.Infrastructure.Persistence.Configurations;

public sealed class GradeEntryConfiguration : IEntityTypeConfiguration<GradeEntry>
{
    public void Configure(EntityTypeBuilder<GradeEntry> builder)
    {
        builder.ToTable("grade_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.StudentUserId).HasColumnName("student_user_id").IsRequired();
        builder.Property(x => x.SubjectId).HasColumnName("subject_id").IsRequired();
        builder.Property(x => x.GradeValue).HasColumnName("grade_value").HasMaxLength(24).IsRequired();
        builder.Property(x => x.Note).HasColumnName("note").HasMaxLength(1000).IsRequired();
        builder.Property(x => x.GradedOn).HasColumnName("graded_on").IsRequired();
    }
}
