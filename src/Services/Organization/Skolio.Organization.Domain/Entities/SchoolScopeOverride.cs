using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Domain.Entities;

/// <summary>
/// Optional per-school restriction subset over the default SchoolType matrix.
/// Overrides can only DISABLE capabilities that are enabled in the default matrix.
/// null = inherit from matrix, false = explicitly disabled.
/// </summary>
public sealed class SchoolScopeOverride
{
    private SchoolScopeOverride()
    {
    }

    private SchoolScopeOverride(
        Guid id,
        Guid schoolId,
        Guid matrixId,
        bool? overrideUsesClasses,
        bool? overrideUsesGroups,
        bool? overrideUsesSubjects,
        bool? overrideUsesFieldOfStudy,
        bool? overrideUsesDailyReports,
        bool? overrideUsesAttendance,
        bool? overrideUsesGrades,
        bool? overrideUsesHomework)
    {
        if (id == Guid.Empty)
        {
            throw new OrganizationDomainException("School scope override id is required.");
        }

        if (schoolId == Guid.Empty)
        {
            throw new OrganizationDomainException("School scope override school id is required.");
        }

        if (matrixId == Guid.Empty)
        {
            throw new OrganizationDomainException("School scope override matrix id is required.");
        }

        Id = id;
        SchoolId = schoolId;
        MatrixId = matrixId;
        OverrideUsesClasses = overrideUsesClasses;
        OverrideUsesGroups = overrideUsesGroups;
        OverrideUsesSubjects = overrideUsesSubjects;
        OverrideUsesFieldOfStudy = overrideUsesFieldOfStudy;
        OverrideUsesDailyReports = overrideUsesDailyReports;
        OverrideUsesAttendance = overrideUsesAttendance;
        OverrideUsesGrades = overrideUsesGrades;
        OverrideUsesHomework = overrideUsesHomework;
    }

    public Guid Id { get; private set; }
    public Guid SchoolId { get; private set; }
    public Guid MatrixId { get; private set; }

    /// <summary>null = inherit, false = disabled</summary>
    public bool? OverrideUsesClasses { get; private set; }
    /// <summary>null = inherit, false = disabled</summary>
    public bool? OverrideUsesGroups { get; private set; }
    /// <summary>null = inherit, false = disabled</summary>
    public bool? OverrideUsesSubjects { get; private set; }
    /// <summary>null = inherit, false = disabled</summary>
    public bool? OverrideUsesFieldOfStudy { get; private set; }
    /// <summary>null = inherit, false = disabled</summary>
    public bool? OverrideUsesDailyReports { get; private set; }
    /// <summary>null = inherit, false = disabled</summary>
    public bool? OverrideUsesAttendance { get; private set; }
    /// <summary>null = inherit, false = disabled</summary>
    public bool? OverrideUsesGrades { get; private set; }
    /// <summary>null = inherit, false = disabled</summary>
    public bool? OverrideUsesHomework { get; private set; }

    public School School { get; private set; } = null!;
    public SchoolContextScopeMatrix Matrix { get; private set; } = null!;

    public static SchoolScopeOverride Create(
        Guid id,
        Guid schoolId,
        Guid matrixId,
        bool? overrideUsesClasses = null,
        bool? overrideUsesGroups = null,
        bool? overrideUsesSubjects = null,
        bool? overrideUsesFieldOfStudy = null,
        bool? overrideUsesDailyReports = null,
        bool? overrideUsesAttendance = null,
        bool? overrideUsesGrades = null,
        bool? overrideUsesHomework = null)
        => new(id, schoolId, matrixId, overrideUsesClasses, overrideUsesGroups, overrideUsesSubjects,
            overrideUsesFieldOfStudy, overrideUsesDailyReports, overrideUsesAttendance,
            overrideUsesGrades, overrideUsesHomework);
}
