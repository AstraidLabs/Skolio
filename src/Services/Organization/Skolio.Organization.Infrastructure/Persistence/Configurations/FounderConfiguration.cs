using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence.Configurations;

public sealed class FounderConfiguration : IEntityTypeConfiguration<Founder>
{
    public void Configure(EntityTypeBuilder<Founder> builder)
    {
        builder.ToTable("founders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.FounderType).HasColumnName("founder_type").HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(x => x.FounderCategory).HasColumnName("founder_category").HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(x => x.FounderName).HasColumnName("founder_name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.FounderLegalForm).HasColumnName("founder_legal_form").HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(x => x.FounderIco).HasColumnName("founder_ico").HasMaxLength(32);
        builder.Property(x => x.FounderEmail).HasColumnName("founder_email").HasMaxLength(256);
        builder.Property(x => x.FounderDataBox).HasColumnName("founder_data_box").HasMaxLength(64);

        builder.OwnsOne(x => x.FounderAddress, address =>
        {
            address.Property(x => x.Street).HasColumnName("founder_address_street").HasMaxLength(200).IsRequired();
            address.Property(x => x.City).HasColumnName("founder_address_city").HasMaxLength(120).IsRequired();
            address.Property(x => x.PostalCode).HasColumnName("founder_address_postal_code").HasMaxLength(32).IsRequired();
            address.Property(x => x.Country).HasColumnName("founder_address_country").HasMaxLength(120).IsRequired();
        });

        builder.HasIndex(x => x.FounderType);
        builder.HasIndex(x => x.FounderCategory);
        builder.HasIndex(x => x.FounderIco).IsUnique().HasFilter("founder_ico IS NOT NULL");
    }
}
