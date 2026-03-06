using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Identity.Domain.Entities;

namespace Skolio.Identity.Infrastructure.Persistence.Configurations;

public sealed class ParentStudentLinkConfiguration : IEntityTypeConfiguration<ParentStudentLink>
{
    public void Configure(EntityTypeBuilder<ParentStudentLink> builder)
    {
        builder.ToTable("parent_student_links");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ParentUserProfileId).HasColumnName("parent_user_profile_id").IsRequired();
        builder.Property(x => x.StudentUserProfileId).HasColumnName("student_user_profile_id").IsRequired();
        builder.Property(x => x.Relationship).HasColumnName("relationship").HasMaxLength(64).IsRequired();
    }
}
