using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Academics.Domain.Entities;

namespace Skolio.Academics.Infrastructure.Persistence.Configurations;

public sealed class LessonRecordConfiguration : IEntityTypeConfiguration<LessonRecord>
{
    public void Configure(EntityTypeBuilder<LessonRecord> builder)
    {
        builder.ToTable("lesson_records");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TimetableEntryId).HasColumnName("timetable_entry_id").IsRequired();
        builder.Property(x => x.LessonDate).HasColumnName("lesson_date").IsRequired();
        builder.Property(x => x.Topic).HasColumnName("topic").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Summary).HasColumnName("summary").HasMaxLength(1500).IsRequired();
    }
}
