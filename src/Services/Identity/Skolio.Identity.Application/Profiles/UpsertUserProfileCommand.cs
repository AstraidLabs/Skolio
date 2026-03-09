using MediatR;
using Skolio.Identity.Application.Contracts;
using Skolio.Identity.Domain.Enums;

namespace Skolio.Identity.Application.Profiles;

public sealed record UpsertUserProfileCommand(
    Guid? UserProfileId,
    string FirstName,
    string LastName,
    UserType UserType,
    string Email,
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
    string? TeacherRoleLabel,
    string? QualificationSummary,
    string? SchoolContextSummary,
    string? ParentRelationshipSummary,
    string? DeliveryContactName,
    string? DeliveryContactPhone,
    string? PreferredContactChannel,
    string? CommunicationPreferencesSummary,
    string? PublicContactNote,
    string? PreferredContactNote) : IRequest<UserProfileContract>;
