using MediatR;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Domain.Enums;

namespace Skolio.Organization.Application.Schools;

public sealed record CreateSchoolCommand(
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
    PlatformStatus PlatformStatus,
    SchoolOperatorContract SchoolOperator,
    FounderContract Founder) : IRequest<SchoolContract>;
