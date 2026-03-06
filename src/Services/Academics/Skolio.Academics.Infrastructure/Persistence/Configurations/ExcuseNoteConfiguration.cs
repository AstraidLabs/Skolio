using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Academics.Domain.Entities;

namespace Skolio.Academics.Infrastructure.Persistence.Configurations;

public sealed class ExcuseNoteConfiguration : IEntityTypeConfiguration<ExcuseNote>
{
    public void Configure(EntityTypeBuilder<ExcuseNote> builder)
    {
        builder.ToTable("excuse_notes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.AttendanceRecordId).HasColumnName("attendance_record_id").IsRequired();
        builder.Property(x => x.ParentUserId).HasColumnName("parent_user_id").IsRequired();
        builder.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(1000).IsRequired();
        builder.Property(x => x.SubmittedAtUtc).HasColumnName("submitted_at_utc").IsRequired();
    }
}
