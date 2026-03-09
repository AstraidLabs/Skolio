using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Domain.Exceptions;
using Skolio.Organization.Domain.ValueObjects;

namespace Skolio.Organization.Domain.Entities;

public sealed class School
{
    private readonly List<SchoolYear> _schoolYears = [];

    private School()
    {
    }

    private School(
        Guid id,
        string name,
        SchoolType schoolType,
        SchoolKind schoolKind,
        string? schoolIzo,
        string? schoolEmail,
        string? schoolPhone,
        string? schoolWebsite,
        Address mainAddress,
        string? educationLocationsSummary,
        DateOnly? registryEntryDate,
        DateOnly? educationStartDate,
        int? maxStudentCapacity,
        string? teachingLanguage,
        Guid schoolOperatorId,
        Guid founderId,
        PlatformStatus platformStatus)
    {
        Id = id;
        SetName(name);
        SetSchoolType(schoolType);
        SchoolKind = schoolKind;
        MainAddress = mainAddress;
        IsActive = true;
        UpdateIdentityAndOperations(
            schoolKind,
            schoolIzo,
            schoolEmail,
            schoolPhone,
            schoolWebsite,
            mainAddress,
            educationLocationsSummary,
            registryEntryDate,
            educationStartDate,
            maxStudentCapacity,
            teachingLanguage,
            schoolOperatorId,
            founderId,
            platformStatus);
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public SchoolType SchoolType { get; private set; }
    public SchoolKind SchoolKind { get; private set; }
    public string? SchoolIzo { get; private set; }
    public string? SchoolEmail { get; private set; }
    public string? SchoolPhone { get; private set; }
    public string? SchoolWebsite { get; private set; }
    public Address MainAddress { get; private set; } = Address.Create("Unknown", "Unknown", "00000", "CZ");
    public string? EducationLocationsSummary { get; private set; }
    public DateOnly? RegistryEntryDate { get; private set; }
    public DateOnly? EducationStartDate { get; private set; }
    public int? MaxStudentCapacity { get; private set; }
    public string? TeachingLanguage { get; private set; }
    public Guid? SchoolOperatorId { get; private set; }
    public SchoolOperator? SchoolOperator { get; private set; }
    public Guid? FounderId { get; private set; }
    public Founder? Founder { get; private set; }
    public PlatformStatus PlatformStatus { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? SchoolAdministratorUserProfileId { get; private set; }
    public IReadOnlyCollection<SchoolYear> SchoolYears => _schoolYears;

    public static School Create(
        Guid id,
        string name,
        SchoolType schoolType,
        SchoolKind schoolKind,
        string? schoolIzo,
        string? schoolEmail,
        string? schoolPhone,
        string? schoolWebsite,
        Address mainAddress,
        string? educationLocationsSummary,
        DateOnly? registryEntryDate,
        DateOnly? educationStartDate,
        int? maxStudentCapacity,
        string? teachingLanguage,
        Guid schoolOperatorId,
        Guid founderId,
        PlatformStatus platformStatus)
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException("School id is required.");
        }

        return new School(
            id,
            name,
            schoolType,
            schoolKind,
            schoolIzo,
            schoolEmail,
            schoolPhone,
            schoolWebsite,
            mainAddress,
            educationLocationsSummary,
            registryEntryDate,
            educationStartDate,
            maxStudentCapacity,
            teachingLanguage,
            schoolOperatorId,
            founderId,
            platformStatus);
    }

    public void Rename(string name) => SetName(name);

    public void ChangeSchoolType(SchoolType schoolType) => SetSchoolType(schoolType);

    public void UpdateIdentityAndOperations(
        SchoolKind schoolKind,
        string? schoolIzo,
        string? schoolEmail,
        string? schoolPhone,
        string? schoolWebsite,
        Address mainAddress,
        string? educationLocationsSummary,
        DateOnly? registryEntryDate,
        DateOnly? educationStartDate,
        int? maxStudentCapacity,
        string? teachingLanguage,
        Guid schoolOperatorId,
        Guid founderId,
        PlatformStatus platformStatus)
    {
        if (schoolOperatorId == Guid.Empty)
        {
            throw new OrganizationDomainException("School operator id is required.");
        }

        if (founderId == Guid.Empty)
        {
            throw new OrganizationDomainException("Founder id is required.");
        }

        if (maxStudentCapacity.HasValue && maxStudentCapacity <= 0)
        {
            throw new OrganizationDomainException("Max student capacity must be greater than zero.");
        }

        SchoolKind = schoolKind;
        SchoolIzo = NormalizeOptional(schoolIzo);
        SchoolEmail = NormalizeOptional(schoolEmail);
        SchoolPhone = NormalizeOptional(schoolPhone);
        SchoolWebsite = NormalizeOptional(schoolWebsite);
        MainAddress = mainAddress;
        EducationLocationsSummary = NormalizeOptional(educationLocationsSummary);
        RegistryEntryDate = registryEntryDate;
        EducationStartDate = educationStartDate;
        MaxStudentCapacity = maxStudentCapacity;
        TeachingLanguage = NormalizeOptional(teachingLanguage);
        SchoolOperatorId = schoolOperatorId;
        FounderId = founderId;
        PlatformStatus = platformStatus;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void AssignSchoolAdministrator(Guid userProfileId)
    {
        if (userProfileId == Guid.Empty)
        {
            throw new OrganizationDomainException("School administrator user profile id is required.");
        }

        SchoolAdministratorUserProfileId = userProfileId;
    }

    public SchoolYear AddSchoolYear(Guid schoolYearId, string label, DateOnly startDate, DateOnly endDate)
    {
        if (_schoolYears.Any(x => x.Label.Equals(label, StringComparison.OrdinalIgnoreCase)))
        {
            throw new OrganizationDomainException("School year label must be unique within school.");
        }

        var schoolYear = SchoolYear.Create(schoolYearId, Id, label, startDate, endDate);
        _schoolYears.Add(schoolYear);
        return schoolYear;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new OrganizationDomainException("School name is required.");
        }

        Name = name.Trim();
    }

    private void SetSchoolType(SchoolType schoolType)
    {
        if (!Enum.IsDefined(schoolType))
        {
            throw new OrganizationDomainException("School type is invalid.");
        }

        SchoolType = schoolType;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
