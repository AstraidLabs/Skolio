using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Administration.Domain.Entities;

namespace Skolio.Administration.Infrastructure.Persistence.Configurations;
public sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("system_settings");builder.HasKey(x=>x.Id);builder.Property(x=>x.Id).HasColumnName("id");builder.Property(x=>x.Key).HasColumnName("key").HasMaxLength(120).IsRequired();builder.Property(x=>x.Value).HasColumnName("value").HasMaxLength(2000).IsRequired();builder.Property(x=>x.IsSensitive).HasColumnName("is_sensitive").IsRequired();
    }
}
