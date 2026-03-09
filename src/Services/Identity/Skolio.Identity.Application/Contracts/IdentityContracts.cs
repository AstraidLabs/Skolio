using Skolio.Identity.Domain.Enums;

namespace Skolio.Identity.Application.Contracts;

public sealed record UserProfileContract(
    Guid Id,
    string FirstName,
    string LastName,
    UserType UserType,
    string Email,
    bool IsActive,
    string? PreferredDisplayName,
    string? PreferredLanguage,
    string? PhoneNumber,
    string? Gender,
    DateOnly? DateOfBirth,
    string? NationalIdNumber,
    string? BirthPlace,
    string? PermanentAddress,
    string? CorrespondenceAddress,
    string? ContactEmail,
    string? LegalGuardian1,
    string? LegalGuardian2,
    string? SchoolPlacement,
    string? HealthInsuranceProvider,
    string? Pediatrician,
    string? HealthSafetyNotes,
    string? SupportMeasuresSummary,
    string? PositionTitle,
    string? PublicContactNote,
    string? PreferredContactNote);
public sealed record SchoolRoleAssignmentContract(Guid Id, Guid UserProfileId, Guid SchoolId, string RoleCode);
public sealed record ParentStudentLinkContract(Guid Id, Guid ParentUserProfileId, Guid StudentUserProfileId, string Relationship);
