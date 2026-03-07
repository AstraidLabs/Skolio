using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Domain.Entities;

public sealed class GradeLevel
{
    private GradeLevel(Guid id, Guid schoolId, int level, string displayName)
    {
        Id = id;
        SchoolId = schoolId;
        SetLevel(level);
        SetDisplayName(displayName);
    }

    public Guid Id { get; }
    public Guid SchoolId { get; }
    public int Level { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;

    public static GradeLevel Create(Guid id, Guid schoolId, SchoolType schoolType, int level, string displayName)
    {
        if (id == Guid.Empty || schoolId == Guid.Empty)
        {
            throw new OrganizationDomainException("Grade level id and school id are required.");
        }

        ValidateLevelRange(schoolType, level);
        return new GradeLevel(id, schoolId, level, displayName);
    }

    public void Update(SchoolType schoolType, int level, string displayName)
    {
        ValidateLevelRange(schoolType, level);
        SetLevel(level);
        SetDisplayName(displayName);
    }

    private static void ValidateLevelRange(SchoolType schoolType, int level)
    {
        var isValid = schoolType switch
        {
            SchoolType.Kindergarten => level is >= 0 and <= 3,
            SchoolType.ElementarySchool => level is >= 1 and <= 9,
            SchoolType.SecondarySchool => level is >= 1 and <= 4,
            _ => false
        };

        if (!isValid)
        {
            throw new OrganizationDomainException($"Grade level {level} is not valid for school type {schoolType}.");
        }
    }

    private void SetLevel(int level)
    {
        if (level < 0)
        {
            throw new OrganizationDomainException("Grade level must be positive or zero.");
        }

        Level = level;
    }

    private void SetDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new OrganizationDomainException("Grade level display name is required.");
        }

        DisplayName = displayName.Trim();
    }
}
