using Skolio.Organization.Domain.Entities;
using Skolio.Organization.Domain.Enums;

namespace Skolio.Organization.Application.Abstractions;

public interface IOrganizationReadStore
{
    Task<School?> GetSchoolAsync(Guid schoolId, CancellationToken cancellationToken);
    Task<SchoolContextScopeMatrix?> GetSchoolContextMatrixBySchoolTypeAsync(SchoolType schoolType, CancellationToken cancellationToken);
    Task<SchoolScopeOverride?> GetSchoolScopeOverrideAsync(Guid schoolId, CancellationToken cancellationToken);
    Task<List<SchoolPlaceOfEducation>> GetSchoolPlacesOfEducationAsync(Guid schoolId, CancellationToken cancellationToken);
    Task<List<SchoolCapacity>> GetSchoolCapacitiesAsync(Guid schoolId, CancellationToken cancellationToken);
    Task<List<RoleDefinition>> GetRoleDefinitionsAsync(CancellationToken cancellationToken);
    Task<OrganizationSchoolStructureMatrixEntry?> GetSchoolStructureMatrixByMatrixIdAsync(Guid matrixId, CancellationToken cancellationToken);
    Task<OrganizationRegistryMatrixEntry?> GetRegistryMatrixByMatrixIdAsync(Guid matrixId, CancellationToken cancellationToken);
    Task<OrganizationCapacityMatrixEntry?> GetCapacityMatrixByMatrixIdAsync(Guid matrixId, CancellationToken cancellationToken);
    Task<OrganizationAcademicStructureMatrixEntry?> GetAcademicStructureMatrixByMatrixIdAsync(Guid matrixId, CancellationToken cancellationToken);
    Task<OrganizationAssignmentMatrixEntry?> GetAssignmentMatrixByMatrixIdAsync(Guid matrixId, CancellationToken cancellationToken);
}
