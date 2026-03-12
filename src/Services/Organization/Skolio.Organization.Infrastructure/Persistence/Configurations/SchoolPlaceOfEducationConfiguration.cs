using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence.Configurations;

public sealed class SchoolPlaceOfEducationConfiguration : IEntityTypeConfiguration<SchoolPlaceOfEducation>
{
    public void Configure(EntityTypeBuilder<SchoolPlaceOfEducation> builder)
    {
        builder.ToTable("school_places_of_education");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.SchoolId).HasColumnName("school_id").IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(x => x.IsPrimary).HasColumnName("is_primary").HasDefaultValue(false).IsRequired();

        builder.OwnsOne(x => x.Address, address =>
        {
            address.Property(x => x.Street).HasColumnName("address_street").HasMaxLength(200).IsRequired();
            address.Property(x => x.City).HasColumnName("address_city").HasMaxLength(120).IsRequired();
            address.Property(x => x.PostalCode).HasColumnName("address_postal_code").HasMaxLength(32).IsRequired();
            address.Property(x => x.Country).HasColumnName("address_country").HasMaxLength(120).IsRequired();
        });

        builder.HasOne(x => x.School)
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SchoolId);
        builder.HasIndex(x => new { x.SchoolId, x.IsPrimary }).HasFilter("is_primary = true").IsUnique();
    }
}
