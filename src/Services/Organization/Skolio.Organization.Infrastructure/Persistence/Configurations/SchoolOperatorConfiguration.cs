using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence.Configurations;

public sealed class SchoolOperatorConfiguration : IEntityTypeConfiguration<SchoolOperator>
{
    public void Configure(EntityTypeBuilder<SchoolOperator> builder)
    {
        builder.ToTable("school_operators");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.LegalEntityName).HasColumnName("legal_entity_name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.LegalForm).HasColumnName("legal_form").HasConversion<string>().HasMaxLength(64).IsRequired();
        builder.Property(x => x.CompanyNumberIco).HasColumnName("company_number_ico").HasMaxLength(32);
        builder.Property(x => x.RedIzo).HasColumnName("red_izo").HasMaxLength(32);
        builder.Property(x => x.OperatorEmail).HasColumnName("operator_email").HasMaxLength(256);
        builder.Property(x => x.DataBox).HasColumnName("data_box").HasMaxLength(64);
        builder.Property(x => x.ResortIdentifier).HasColumnName("resort_identifier").HasMaxLength(64);
        builder.Property(x => x.DirectorSummary).HasColumnName("director_summary").HasMaxLength(300);
        builder.Property(x => x.StatutoryBodySummary).HasColumnName("statutory_body_summary").HasMaxLength(600);

        builder.OwnsOne(x => x.RegisteredOfficeAddress, address =>
        {
            address.Property(x => x.Street).HasColumnName("registered_office_street").HasMaxLength(200).IsRequired();
            address.Property(x => x.City).HasColumnName("registered_office_city").HasMaxLength(120).IsRequired();
            address.Property(x => x.PostalCode).HasColumnName("registered_office_postal_code").HasMaxLength(32).IsRequired();
            address.Property(x => x.Country).HasColumnName("registered_office_country").HasMaxLength(120).IsRequired();
        });

        builder.HasIndex(x => x.LegalEntityName);
        builder.HasIndex(x => x.CompanyNumberIco);
        builder.HasIndex(x => x.RedIzo);
    }
}
