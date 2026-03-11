using Skolio.Organization.Domain.Enums;

namespace Skolio.Organization.Application.Contracts;

public sealed record ResolvedSchoolContextContract(
    Guid SchoolId,
    SchoolType SchoolType,
    Guid MatrixId,
    bool UsesClasses,
    bool UsesGroups,
    bool UsesSubjects,
    bool UsesFieldOfStudy,
    bool UsesDailyReports,
    bool UsesAttendance,
    bool UsesGrades,
    bool UsesHomework,
    IReadOnlyList<string> AllowedRoles,
    IReadOnlyList<string> AllowedProfileSections,
    IReadOnlyList<string> AllowedCreateUserFlows,
    IReadOnlyList<string> AllowedUserManagementFlows,
    IReadOnlyList<string> AllowedOrganizationSections,
    IReadOnlyList<string> AllowedAcademicsSections,
    bool HasSchoolScopeOverride);
