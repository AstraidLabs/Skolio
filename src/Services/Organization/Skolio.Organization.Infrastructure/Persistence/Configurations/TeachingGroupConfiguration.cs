using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence.Configurations;

public sealed class TeachingGroupConfiguration : IEntityTypeConfiguration<TeachingGroup>
{
    public void Configure(EntityTypeBuilder<TeachingGroup> builder)
    {
        builder.ToTable("teaching_groups");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(x => x.ClassRoomId).HasColumnName("class_room_id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        builder.Property(x => x.IsDailyOperationsGroup).HasColumnName("is_daily_operations_group").IsRequired();
    }
}
