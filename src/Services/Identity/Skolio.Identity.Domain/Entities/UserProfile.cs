using Skolio.Identity.Domain.Enums;
using Skolio.Identity.Domain.Exceptions;

namespace Skolio.Identity.Domain.Entities;

public sealed class UserProfile
{
    private UserProfile(
        Guid id,
        string firstName,
        string lastName,
        UserType userType,
        string email,
        string? preferredDisplayName,
        string? preferredLanguage,
        string? phoneNumber,
        string? gender,
        DateOnly? dateOfBirth,
        string? nationalIdNumber,
        string? birthPlace,
        string? permanentAddress,
        string? correspondenceAddress,
        string? contactEmail,
        string? legalGuardian1,
        string? legalGuardian2,
        string? schoolPlacement,
        string? healthInsuranceProvider,
        string? pediatrician,
        string? healthSafetyNotes,
        string? supportMeasuresSummary,
        string? positionTitle,
        string? teacherRoleLabel,
        string? qualificationSummary,
        string? schoolContextSummary,
        string? publicContactNote,
        string? preferredContactNote)
    {
        Id = id;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        UserType = userType;
        Email = email.Trim();
        PreferredDisplayName = NormalizeOptional(preferredDisplayName);
        PreferredLanguage = NormalizeOptional(preferredLanguage);
        PhoneNumber = NormalizeOptional(phoneNumber);
        Gender = NormalizeOptional(gender);
        DateOfBirth = dateOfBirth;
        NationalIdNumber = NormalizeOptional(nationalIdNumber);
        BirthPlace = NormalizeOptional(birthPlace);
        PermanentAddress = NormalizeOptional(permanentAddress);
        CorrespondenceAddress = NormalizeOptional(correspondenceAddress);
        ContactEmail = NormalizeOptional(contactEmail);
        LegalGuardian1 = NormalizeOptional(legalGuardian1);
        LegalGuardian2 = NormalizeOptional(legalGuardian2);
        SchoolPlacement = NormalizeOptional(schoolPlacement);
        HealthInsuranceProvider = NormalizeOptional(healthInsuranceProvider);
        Pediatrician = NormalizeOptional(pediatrician);
        HealthSafetyNotes = NormalizeOptional(healthSafetyNotes);
        SupportMeasuresSummary = NormalizeOptional(supportMeasuresSummary);
        PositionTitle = NormalizeOptional(positionTitle);
        TeacherRoleLabel = NormalizeOptional(teacherRoleLabel);
        QualificationSummary = NormalizeOptional(qualificationSummary);
        SchoolContextSummary = NormalizeOptional(schoolContextSummary);
        PublicContactNote = NormalizeOptional(publicContactNote);
        PreferredContactNote = NormalizeOptional(preferredContactNote);
        IsActive = true;
    }

    public Guid Id { get; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public UserType UserType { get; private set; }
    public string Email { get; private set; }
    public string? PreferredDisplayName { get; private set; }
    public string? PreferredLanguage { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Gender { get; private set; }
    public DateOnly? DateOfBirth { get; private set; }
    public string? NationalIdNumber { get; private set; }
    public string? BirthPlace { get; private set; }
    public string? PermanentAddress { get; private set; }
    public string? CorrespondenceAddress { get; private set; }
    public string? ContactEmail { get; private set; }
    public string? LegalGuardian1 { get; private set; }
    public string? LegalGuardian2 { get; private set; }
    public string? SchoolPlacement { get; private set; }
    public string? HealthInsuranceProvider { get; private set; }
    public string? Pediatrician { get; private set; }
    public string? HealthSafetyNotes { get; private set; }
    public string? SupportMeasuresSummary { get; private set; }
    public string? PositionTitle { get; private set; }
    public string? TeacherRoleLabel { get; private set; }
    public string? QualificationSummary { get; private set; }
    public string? SchoolContextSummary { get; private set; }
    public string? PublicContactNote { get; private set; }
    public string? PreferredContactNote { get; private set; }
    public bool IsActive { get; private set; }

    public static UserProfile Create(
        Guid id,
        string firstName,
        string lastName,
        UserType userType,
        string email,
        string? preferredDisplayName = null,
        string? preferredLanguage = null,
        string? phoneNumber = null,
        string? gender = null,
        DateOnly? dateOfBirth = null,
        string? nationalIdNumber = null,
        string? birthPlace = null,
        string? permanentAddress = null,
        string? correspondenceAddress = null,
        string? contactEmail = null,
        string? legalGuardian1 = null,
        string? legalGuardian2 = null,
        string? schoolPlacement = null,
        string? healthInsuranceProvider = null,
        string? pediatrician = null,
        string? healthSafetyNotes = null,
        string? supportMeasuresSummary = null,
        string? positionTitle = null,
        string? teacherRoleLabel = null,
        string? qualificationSummary = null,
        string? schoolContextSummary = null,
        string? publicContactNote = null,
        string? preferredContactNote = null)
    {
        if (id == Guid.Empty)
            throw new IdentityDomainException("User profile id is required.");
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email))
            throw new IdentityDomainException("User profile first name, last name and email are required.");

        return new UserProfile(
            id,
            firstName,
            lastName,
            userType,
            email,
            preferredDisplayName,
            preferredLanguage,
            phoneNumber,
            gender,
            dateOfBirth,
            nationalIdNumber,
            birthPlace,
            permanentAddress,
            correspondenceAddress,
            contactEmail,
            legalGuardian1,
            legalGuardian2,
            schoolPlacement,
            healthInsuranceProvider,
            pediatrician,
            healthSafetyNotes,
            supportMeasuresSummary,
            positionTitle,
            teacherRoleLabel,
            qualificationSummary,
            schoolContextSummary,
            publicContactNote,
            preferredContactNote);
    }

    public void Update(
        string firstName,
        string lastName,
        UserType userType,
        string email,
        string? preferredDisplayName,
        string? preferredLanguage,
        string? phoneNumber,
        string? gender,
        DateOnly? dateOfBirth,
        string? nationalIdNumber,
        string? birthPlace,
        string? permanentAddress,
        string? correspondenceAddress,
        string? contactEmail,
        string? legalGuardian1,
        string? legalGuardian2,
        string? schoolPlacement,
        string? healthInsuranceProvider,
        string? pediatrician,
        string? healthSafetyNotes,
        string? supportMeasuresSummary,
        string? positionTitle,
        string? teacherRoleLabel,
        string? qualificationSummary,
        string? schoolContextSummary,
        string? publicContactNote,
        string? preferredContactNote)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName) || string.IsNullOrWhiteSpace(email))
            throw new IdentityDomainException("User profile first name, last name and email are required.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        UserType = userType;
        Email = email.Trim();
        PreferredDisplayName = NormalizeOptional(preferredDisplayName);
        PreferredLanguage = NormalizeOptional(preferredLanguage);
        PhoneNumber = NormalizeOptional(phoneNumber);
        Gender = NormalizeOptional(gender);
        DateOfBirth = dateOfBirth;
        NationalIdNumber = NormalizeOptional(nationalIdNumber);
        BirthPlace = NormalizeOptional(birthPlace);
        PermanentAddress = NormalizeOptional(permanentAddress);
        CorrespondenceAddress = NormalizeOptional(correspondenceAddress);
        ContactEmail = NormalizeOptional(contactEmail);
        LegalGuardian1 = NormalizeOptional(legalGuardian1);
        LegalGuardian2 = NormalizeOptional(legalGuardian2);
        SchoolPlacement = NormalizeOptional(schoolPlacement);
        HealthInsuranceProvider = NormalizeOptional(healthInsuranceProvider);
        Pediatrician = NormalizeOptional(pediatrician);
        HealthSafetyNotes = NormalizeOptional(healthSafetyNotes);
        SupportMeasuresSummary = NormalizeOptional(supportMeasuresSummary);
        PositionTitle = NormalizeOptional(positionTitle);
        TeacherRoleLabel = NormalizeOptional(teacherRoleLabel);
        QualificationSummary = NormalizeOptional(qualificationSummary);
        SchoolContextSummary = NormalizeOptional(schoolContextSummary);
        PublicContactNote = NormalizeOptional(publicContactNote);
        PreferredContactNote = NormalizeOptional(preferredContactNote);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
