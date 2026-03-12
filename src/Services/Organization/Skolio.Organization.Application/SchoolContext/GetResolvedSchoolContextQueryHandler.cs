using MediatR;
using Skolio.Organization.Application.Abstractions;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Domain.Entities;
using Skolio.Organization.Domain.Enums;

namespace Skolio.Organization.Application.SchoolContext;

public sealed class GetResolvedSchoolContextQueryHandler(IOrganizationReadStore readStore)
    : IRequestHandler<GetResolvedSchoolContextQuery, ResolvedSchoolContextContract?>
{
    public async Task<ResolvedSchoolContextContract?> Handle(
        GetResolvedSchoolContextQuery request,
        CancellationToken cancellationToken)
    {
        var school = await readStore.GetSchoolAsync(request.SchoolId, cancellationToken);
        if (school is null)
        {
            return null;
        }

        var matrix = await readStore.GetSchoolContextMatrixBySchoolTypeAsync(school.SchoolType, cancellationToken);
        if (matrix is null)
        {
            return null;
        }

        var override_ = await readStore.GetSchoolScopeOverrideAsync(request.SchoolId, cancellationToken);

        var structureMatrix = await readStore.GetSchoolStructureMatrixByMatrixIdAsync(matrix.Id, cancellationToken);
        var registryMatrix = await readStore.GetRegistryMatrixByMatrixIdAsync(matrix.Id, cancellationToken);
        var academicMatrix = await readStore.GetAcademicStructureMatrixByMatrixIdAsync(matrix.Id, cancellationToken);
        var assignmentMatrix = await readStore.GetAssignmentMatrixByMatrixIdAsync(matrix.Id, cancellationToken);

        return Resolve(school, matrix, override_, structureMatrix, registryMatrix, academicMatrix, assignmentMatrix);
    }

    private static ResolvedSchoolContextContract Resolve(
        School school,
        SchoolContextScopeMatrix matrix,
        SchoolScopeOverride? scopeOverride,
        OrganizationSchoolStructureMatrixEntry? structureMatrix,
        OrganizationRegistryMatrixEntry? registryMatrix,
        OrganizationAcademicStructureMatrixEntry? academicMatrix,
        OrganizationAssignmentMatrixEntry? assignmentMatrix)
    {
        bool GetCapability(ScopeCapabilityCode code)
            => matrix.Capabilities.FirstOrDefault(c => c.CapabilityCode == code)?.IsEnabled ?? false;

        bool usesClasses = ApplyOverride(GetCapability(ScopeCapabilityCode.UsesClasses), scopeOverride?.OverrideUsesClasses);
        bool usesGroups = ApplyOverride(GetCapability(ScopeCapabilityCode.UsesGroups), scopeOverride?.OverrideUsesGroups);
        bool usesSubjects = ApplyOverride(GetCapability(ScopeCapabilityCode.UsesSubjects), scopeOverride?.OverrideUsesSubjects);
        bool usesFieldOfStudy = ApplyOverride(GetCapability(ScopeCapabilityCode.UsesFieldOfStudy), scopeOverride?.OverrideUsesFieldOfStudy);
        bool usesDailyReports = ApplyOverride(GetCapability(ScopeCapabilityCode.UsesDailyReports), scopeOverride?.OverrideUsesDailyReports);
        bool usesAttendance = ApplyOverride(GetCapability(ScopeCapabilityCode.UsesAttendance), scopeOverride?.OverrideUsesAttendance);
        bool usesGrades = ApplyOverride(GetCapability(ScopeCapabilityCode.UsesGrades), scopeOverride?.OverrideUsesGrades);
        bool usesHomework = ApplyOverride(GetCapability(ScopeCapabilityCode.UsesHomework), scopeOverride?.OverrideUsesHomework);

        var resolvedStructure = structureMatrix is not null
            ? new ResolvedSchoolStructureContract(
                structureMatrix.UsesGradeLevels,
                structureMatrix.UsesClasses,
                structureMatrix.UsesGroups,
                structureMatrix.GroupIsPrimaryStructure)
            : null;

        var resolvedRegistry = registryMatrix is not null
            ? new ResolvedRegistryContract(
                registryMatrix.RequiresIzo,
                registryMatrix.RequiresRedIzo,
                registryMatrix.RequiresIco,
                registryMatrix.RequiresDataBox,
                registryMatrix.RequiresFounder,
                registryMatrix.RequiresTeachingLanguage)
            : null;

        var resolvedAcademic = academicMatrix is not null
            ? new ResolvedAcademicStructureContract(
                academicMatrix.UsesSubjects,
                academicMatrix.UsesFieldOfStudy,
                academicMatrix.SubjectIsClassBound,
                academicMatrix.FieldOfStudyIsRequired)
            : null;

        var resolvedAssignment = assignmentMatrix is not null
            ? new ResolvedAssignmentContract(
                assignmentMatrix.AllowsClassRoomAssignment,
                assignmentMatrix.AllowsGroupAssignment,
                assignmentMatrix.AllowsSubjectAssignment,
                assignmentMatrix.StudentRequiresClassPlacement,
                assignmentMatrix.StudentRequiresGroupPlacement)
            : null;

        return new ResolvedSchoolContextContract(
            SchoolId: school.Id,
            SchoolType: school.SchoolType,
            MatrixId: matrix.Id,
            UsesClasses: usesClasses,
            UsesGroups: usesGroups,
            UsesSubjects: usesSubjects,
            UsesFieldOfStudy: usesFieldOfStudy,
            UsesDailyReports: usesDailyReports,
            UsesAttendance: usesAttendance,
            UsesGrades: usesGrades,
            UsesHomework: usesHomework,
            AllowedRoles: matrix.AllowedRoles.Select(r => r.RoleCode).ToList().AsReadOnly(),
            AllowedProfileSections: matrix.AllowedProfileSections.Select(s => s.SectionCode.ToString()).ToList().AsReadOnly(),
            AllowedCreateUserFlows: matrix.AllowedCreateUserFlows.Select(f => f.FlowCode.ToString()).ToList().AsReadOnly(),
            AllowedUserManagementFlows: matrix.AllowedUserManagementFlows.Select(f => f.FlowCode.ToString()).ToList().AsReadOnly(),
            AllowedOrganizationSections: matrix.AllowedOrganizationSections.Select(s => s.SectionCode.ToString()).ToList().AsReadOnly(),
            AllowedAcademicsSections: matrix.AllowedAcademicsSections.Select(s => s.SectionCode.ToString()).ToList().AsReadOnly(),
            HasSchoolScopeOverride: scopeOverride is not null,
            SchoolStructure: resolvedStructure,
            Registry: resolvedRegistry,
            AcademicStructure: resolvedAcademic,
            Assignment: resolvedAssignment);
    }

    /// <summary>
    /// Override can only restrict (false). null means inherit from matrix default.
    /// </summary>
    private static bool ApplyOverride(bool matrixDefault, bool? overrideValue)
        => overrideValue.HasValue ? overrideValue.Value && matrixDefault : matrixDefault;
}
