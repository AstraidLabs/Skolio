using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Administration.Domain.Entities;

namespace Skolio.Administration.Infrastructure.Persistence.Configurations;
public sealed class SchoolYearLifecyclePolicyConfiguration : IEntityTypeConfiguration<SchoolYearLifecyclePolicy>
{
    public void Configure(EntityTypeBuilder<SchoolYearLifecyclePolicy> builder)
    {
        builder.ToTable("school_year_lifecycle_policies");builder.HasKey(x=>x.Id);builder.Property(x=>x.Id).HasColumnName("id");builder.Property(x=>x.SchoolId).HasColumnName("school_id").IsRequired();builder.Property(x=>x.PolicyName).HasColumnName("policy_name").HasMaxLength(200).IsRequired();builder.Property(x=>x.ClosureGraceDays).HasColumnName("closure_grace_days").IsRequired();builder.Property(x=>x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(16).IsRequired();
    }
}
