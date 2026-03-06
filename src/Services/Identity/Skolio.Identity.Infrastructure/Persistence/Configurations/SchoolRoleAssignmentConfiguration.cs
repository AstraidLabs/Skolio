using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Identity.Domain.Entities;

namespace Skolio.Identity.Infrastructure.Persistence.Configurations;

public sealed class SchoolRoleAssignmentConfiguration : IEntityTypeConfiguration<SchoolRoleAssignment>
{
    public void Configure(EntityTypeBuilder<SchoolRoleAssignment> builder)
    {
        builder.ToTable("school_role_assignments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserProfileId).HasColumnName("user_profile_id").IsRequired();
        builder.Property(x => x.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(x => x.RoleCode).HasColumnName("role_code").HasMaxLength(64).IsRequired();
    }
}
