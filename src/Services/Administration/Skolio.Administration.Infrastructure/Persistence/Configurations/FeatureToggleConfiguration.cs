using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Administration.Domain.Entities;

namespace Skolio.Administration.Infrastructure.Persistence.Configurations;
public sealed class FeatureToggleConfiguration : IEntityTypeConfiguration<FeatureToggle>
{
    public void Configure(EntityTypeBuilder<FeatureToggle> builder)
    {
        builder.ToTable("feature_toggles");builder.HasKey(x=>x.Id);builder.Property(x=>x.Id).HasColumnName("id");builder.Property(x=>x.FeatureCode).HasColumnName("feature_code").HasMaxLength(120).IsRequired();builder.Property(x=>x.IsEnabled).HasColumnName("is_enabled").IsRequired();
    }
}
