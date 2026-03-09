using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Skolio.Identity.Domain.Entities;

namespace Skolio.Identity.Infrastructure.Persistence.Configurations;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(120).IsRequired();
        builder.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(120).IsRequired();
        builder.Property(x => x.UserType).HasColumnName("user_type").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(200).IsRequired();
        builder.Property(x => x.PreferredDisplayName).HasColumnName("preferred_display_name").HasMaxLength(120);
        builder.Property(x => x.PreferredLanguage).HasColumnName("preferred_language").HasMaxLength(16);
        builder.Property(x => x.PhoneNumber).HasColumnName("phone_number").HasMaxLength(32);
        builder.Property(x => x.Gender).HasColumnName("gender").HasMaxLength(16);
        builder.Property(x => x.DateOfBirth).HasColumnName("date_of_birth");
        builder.Property(x => x.NationalIdNumber).HasColumnName("national_id_number").HasMaxLength(32);
        builder.Property(x => x.BirthPlace).HasColumnName("birth_place").HasMaxLength(160);
        builder.Property(x => x.PermanentAddress).HasColumnName("permanent_address").HasMaxLength(240);
        builder.Property(x => x.CorrespondenceAddress).HasColumnName("correspondence_address").HasMaxLength(240);
        builder.Property(x => x.ContactEmail).HasColumnName("contact_email").HasMaxLength(200);
        builder.Property(x => x.LegalGuardian1).HasColumnName("legal_guardian_1").HasMaxLength(240);
        builder.Property(x => x.LegalGuardian2).HasColumnName("legal_guardian_2").HasMaxLength(240);
        builder.Property(x => x.SchoolPlacement).HasColumnName("school_placement").HasMaxLength(240);
        builder.Property(x => x.HealthInsuranceProvider).HasColumnName("health_insurance_provider").HasMaxLength(160);
        builder.Property(x => x.Pediatrician).HasColumnName("pediatrician").HasMaxLength(240);
        builder.Property(x => x.HealthSafetyNotes).HasColumnName("health_safety_notes").HasMaxLength(1000);
        builder.Property(x => x.SupportMeasuresSummary).HasColumnName("support_measures_summary").HasMaxLength(1000);
        builder.Property(x => x.PositionTitle).HasColumnName("position_title").HasMaxLength(120);
        builder.Property(x => x.TeacherRoleLabel).HasColumnName("teacher_role_label").HasMaxLength(120);
        builder.Property(x => x.QualificationSummary).HasColumnName("qualification_summary").HasMaxLength(1000);
        builder.Property(x => x.SchoolContextSummary).HasColumnName("school_context_summary").HasMaxLength(1000);
        builder.Property(x => x.ParentRelationshipSummary).HasColumnName("parent_relationship_summary").HasMaxLength(500);
        builder.Property(x => x.DeliveryContactName).HasColumnName("delivery_contact_name").HasMaxLength(160);
        builder.Property(x => x.DeliveryContactPhone).HasColumnName("delivery_contact_phone").HasMaxLength(32);
        builder.Property(x => x.PreferredContactChannel).HasColumnName("preferred_contact_channel").HasMaxLength(32);
        builder.Property(x => x.CommunicationPreferencesSummary).HasColumnName("communication_preferences_summary").HasMaxLength(500);
        builder.Property(x => x.PublicContactNote).HasColumnName("public_contact_note").HasMaxLength(240);
        builder.Property(x => x.PreferredContactNote).HasColumnName("preferred_contact_note").HasMaxLength(240);
        builder.Property(x => x.AdministrativeWorkDesignation).HasColumnName("administrative_work_designation").HasMaxLength(120);
        builder.Property(x => x.AdministrativeOrganizationSummary).HasColumnName("administrative_organization_summary").HasMaxLength(500);
        builder.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
        builder.HasIndex(x => x.IsActive);
    }
}
