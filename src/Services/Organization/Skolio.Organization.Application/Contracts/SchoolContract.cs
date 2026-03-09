using Skolio.Organization.Domain.Enums;

namespace Skolio.Organization.Application.Contracts;

public sealed record SchoolContract(
    Guid Id,
    string Name,
    SchoolType SchoolType,
    SchoolKind SchoolKind,
    string? SchoolIzo,
    string? SchoolEmail,
    string? SchoolPhone,
    string? SchoolWebsite,
    AddressContract MainAddress,
    string? EducationLocationsSummary,
    DateOnly? RegistryEntryDate,
    DateOnly? EducationStartDate,
    int? MaxStudentCapacity,
    string? TeachingLanguage,
    Guid? SchoolOperatorId,
    Guid? FounderId,
    PlatformStatus PlatformStatus,
    bool IsActive,
    Guid? SchoolAdministratorUserProfileId,
    SchoolOperatorContract? SchoolOperator,
    FounderContract? Founder);
