using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Academics.Domain.Entities;

namespace Skolio.Academics.Infrastructure.Persistence.Configurations;

public sealed class HomeworkAssignmentConfiguration : IEntityTypeConfiguration<HomeworkAssignment>
{
    public void Configure(EntityTypeBuilder<HomeworkAssignment> builder)
    {
        builder.ToTable("homework_assignments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(x => x.AudienceId).HasColumnName("audience_id").IsRequired();
        builder.Property(x => x.SubjectId).HasColumnName("subject_id").IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Instructions).HasColumnName("instructions").HasMaxLength(3000).IsRequired();
        builder.Property(x => x.DueDate).HasColumnName("due_date").IsRequired();
    }
}
