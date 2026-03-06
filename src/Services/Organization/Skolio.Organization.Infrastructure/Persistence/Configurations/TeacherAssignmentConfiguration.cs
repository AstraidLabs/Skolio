using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence.Configurations;

public sealed class TeacherAssignmentConfiguration : IEntityTypeConfiguration<TeacherAssignment>
{
    public void Configure(EntityTypeBuilder<TeacherAssignment> builder)
    {
        builder.ToTable("teacher_assignments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(x => x.TeacherUserId).HasColumnName("teacher_user_id").IsRequired();
        builder.Property(x => x.Scope).HasColumnName("scope").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.ClassRoomId).HasColumnName("class_room_id");
        builder.Property(x => x.TeachingGroupId).HasColumnName("teaching_group_id");
        builder.Property(x => x.SubjectId).HasColumnName("subject_id");
    }
}
