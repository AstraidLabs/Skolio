using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence.Configurations;

public sealed class SchoolConfiguration : IEntityTypeConfiguration<School>
{
    public void Configure(EntityTypeBuilder<School> builder)
    {
        builder.ToTable("schools");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.SchoolType).HasColumnName("school_type").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.SchoolKind).HasColumnName("school_kind").HasConversion<string>().HasMaxLength(32).HasDefaultValue(Skolio.Organization.Domain.Enums.SchoolKind.General).IsRequired();
        builder.Property(x => x.SchoolIzo).HasColumnName("school_izo").HasMaxLength(32);
        builder.Property(x => x.SchoolEmail).HasColumnName("school_email").HasMaxLength(256);
        builder.Property(x => x.SchoolPhone).HasColumnName("school_phone").HasMaxLength(64);
        builder.Property(x => x.SchoolWebsite).HasColumnName("school_website").HasMaxLength(256);
        builder.Property(x => x.EducationLocationsSummary).HasColumnName("education_locations_summary").HasMaxLength(1000);
        builder.Property(x => x.RegistryEntryDate).HasColumnName("registry_entry_date");
        builder.Property(x => x.EducationStartDate).HasColumnName("education_start_date");
        builder.Property(x => x.MaxStudentCapacity).HasColumnName("max_student_capacity");
        builder.Property(x => x.TeachingLanguage).HasColumnName("teaching_language").HasMaxLength(64);
        builder.Property(x => x.PlatformStatus).HasColumnName("platform_status").HasConversion<string>().HasMaxLength(32).HasDefaultValue(Skolio.Organization.Domain.Enums.PlatformStatus.Active).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.Property(x => x.SchoolAdministratorUserProfileId).HasColumnName("school_administrator_user_profile_id");
        builder.Property(x => x.SchoolOperatorId).HasColumnName("school_operator_id");
        builder.Property(x => x.FounderId).HasColumnName("founder_id");

        builder.OwnsOne(x => x.MainAddress, address =>
        {
            address.Property(x => x.Street).HasColumnName("main_address_street").HasMaxLength(200).IsRequired();
            address.Property(x => x.City).HasColumnName("main_address_city").HasMaxLength(120).IsRequired();
            address.Property(x => x.PostalCode).HasColumnName("main_address_postal_code").HasMaxLength(32).IsRequired();
            address.Property(x => x.Country).HasColumnName("main_address_country").HasMaxLength(120).IsRequired();
        });

        builder.HasOne(x => x.SchoolOperator)
            .WithMany()
            .HasForeignKey(x => x.SchoolOperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Founder)
            .WithMany()
            .HasForeignKey(x => x.FounderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.SchoolType);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.SchoolIzo);
    }
}
