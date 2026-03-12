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
    bool HasSchoolScopeOverride,
    ResolvedSchoolStructureContract? SchoolStructure = null,
    ResolvedRegistryContract? Registry = null,
    ResolvedAcademicStructureContract? AcademicStructure = null,
    ResolvedAssignmentContract? Assignment = null);

public sealed record ResolvedSchoolStructureContract(
    bool UsesGradeLevels,
    bool UsesClasses,
    bool UsesGroups,
    bool GroupIsPrimaryStructure);

public sealed record ResolvedRegistryContract(
    bool RequiresIzo,
    bool RequiresRedIzo,
    bool RequiresIco,
    bool RequiresDataBox,
    bool RequiresFounder,
    bool RequiresTeachingLanguage);

public sealed record ResolvedAcademicStructureContract(
    bool UsesSubjects,
    bool UsesFieldOfStudy,
    bool SubjectIsClassBound,
    bool FieldOfStudyIsRequired);

public sealed record ResolvedAssignmentContract(
    bool AllowsClassRoomAssignment,
    bool AllowsGroupAssignment,
    bool AllowsSubjectAssignment,
    bool StudentRequiresClassPlacement,
    bool StudentRequiresGroupPlacement);
