using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Academics.Domain.Entities;

namespace Skolio.Academics.Infrastructure.Persistence.Configurations;

public sealed class TimetableEntryConfiguration : IEntityTypeConfiguration<TimetableEntry>
{
    public void Configure(EntityTypeBuilder<TimetableEntry> builder)
    {
        builder.ToTable("timetable_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(x => x.SchoolYearId).HasColumnName("school_year_id").IsRequired();
        builder.Property(x => x.DayOfWeek).HasColumnName("day_of_week").HasConversion<int>().IsRequired();
        builder.Property(x => x.StartTime).HasColumnName("start_time").IsRequired();
        builder.Property(x => x.EndTime).HasColumnName("end_time").IsRequired();
        builder.Property(x => x.AudienceType).HasColumnName("audience_type").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.AudienceId).HasColumnName("audience_id").IsRequired();
        builder.Property(x => x.SubjectId).HasColumnName("subject_id").IsRequired();
        builder.Property(x => x.TeacherUserId).HasColumnName("teacher_user_id").IsRequired();
    }
}
