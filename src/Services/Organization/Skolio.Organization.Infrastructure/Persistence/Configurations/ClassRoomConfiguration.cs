using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence.Configurations;

public sealed class ClassRoomConfiguration : IEntityTypeConfiguration<ClassRoom>
{
    public void Configure(EntityTypeBuilder<ClassRoom> builder)
    {
        builder.ToTable("class_rooms");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(x => x.GradeLevelId).HasColumnName("grade_level_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(32).IsRequired();
        builder.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(120).IsRequired();
    }
}
