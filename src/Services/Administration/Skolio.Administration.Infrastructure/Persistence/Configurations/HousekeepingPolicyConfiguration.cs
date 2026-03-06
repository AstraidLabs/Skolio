using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Administration.Domain.Entities;

namespace Skolio.Administration.Infrastructure.Persistence.Configurations;
public sealed class HousekeepingPolicyConfiguration : IEntityTypeConfiguration<HousekeepingPolicy>
{
    public void Configure(EntityTypeBuilder<HousekeepingPolicy> builder)
    {
        builder.ToTable("housekeeping_policies");builder.HasKey(x=>x.Id);builder.Property(x=>x.Id).HasColumnName("id");builder.Property(x=>x.PolicyName).HasColumnName("policy_name").HasMaxLength(200).IsRequired();builder.Property(x=>x.RetentionDays).HasColumnName("retention_days").IsRequired();builder.Property(x=>x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16).IsRequired();
    }
}
