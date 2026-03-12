using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Application.Abstractions;
using Skolio.Organization.Domain.Entities;
using Skolio.Organization.Domain.Enums;

namespace Skolio.Organization.Infrastructure.Persistence;

public sealed class OrganizationReadStore(OrganizationDbContext dbContext) : IOrganizationReadStore
{
    public Task<School?> GetSchoolAsync(Guid schoolId, CancellationToken cancellationToken)
        => dbContext.Schools.FirstOrDefaultAsync(x => x.Id == schoolId, cancellationToken);

    public Task<SchoolContextScopeMatrix?> GetSchoolContextMatrixBySchoolTypeAsync(
        SchoolType schoolType,
        CancellationToken cancellationToken)
        => dbContext.SchoolContextScopeMatrices
            .Include(x => x.Capabilities)
            .Include(x => x.AllowedRoles)
            .Include(x => x.AllowedProfileSections)
            .Include(x => x.AllowedCreateUserFlows)
            .Include(x => x.AllowedUserManagementFlows)
            .Include(x => x.AllowedOrganizationSections)
            .Include(x => x.AllowedAcademicsSections)
            .FirstOrDefaultAsync(x => x.SchoolType == schoolType, cancellationToken);

    public Task<SchoolScopeOverride?> GetSchoolScopeOverrideAsync(Guid schoolId, CancellationToken cancellationToken)
        => dbContext.SchoolScopeOverrides
            .FirstOrDefaultAsync(x => x.SchoolId == schoolId, cancellationToken);

    public Task<List<SchoolPlaceOfEducation>> GetSchoolPlacesOfEducationAsync(Guid schoolId, CancellationToken cancellationToken)
        => dbContext.SchoolPlacesOfEducation
            .Where(x => x.SchoolId == schoolId)
            .OrderByDescending(x => x.IsPrimary).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<List<SchoolCapacity>> GetSchoolCapacitiesAsync(Guid schoolId, CancellationToken cancellationToken)
        => dbContext.SchoolCapacities
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.CapacityType)
            .ToListAsync(cancellationToken);

    public Task<List<RoleDefinition>> GetRoleDefinitionsAsync(CancellationToken cancellationToken)
        => dbContext.RoleDefinitions
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

    public Task<OrganizationSchoolStructureMatrixEntry?> GetSchoolStructureMatrixByMatrixIdAsync(Guid matrixId, CancellationToken cancellationToken)
        => dbContext.OrganizationSchoolStructureMatrixEntries
            .FirstOrDefaultAsync(x => x.ParentScopeMatrixId == matrixId, cancellationToken);

    public Task<OrganizationRegistryMatrixEntry?> GetRegistryMatrixByMatrixIdAsync(Guid matrixId, CancellationToken cancellationToken)
        => dbContext.OrganizationRegistryMatrixEntries
            .FirstOrDefaultAsync(x => x.ParentScopeMatrixId == matrixId, cancellationToken);

    public Task<OrganizationCapacityMatrixEntry?> GetCapacityMatrixByMatrixIdAsync(Guid matrixId, CancellationToken cancellationToken)
        => dbContext.OrganizationCapacityMatrixEntries
            .FirstOrDefaultAsync(x => x.ParentScopeMatrixId == matrixId, cancellationToken);

    public Task<OrganizationAcademicStructureMatrixEntry?> GetAcademicStructureMatrixByMatrixIdAsync(Guid matrixId, CancellationToken cancellationToken)
        => dbContext.OrganizationAcademicStructureMatrixEntries
            .FirstOrDefaultAsync(x => x.ParentScopeMatrixId == matrixId, cancellationToken);

    public Task<OrganizationAssignmentMatrixEntry?> GetAssignmentMatrixByMatrixIdAsync(Guid matrixId, CancellationToken cancellationToken)
        => dbContext.OrganizationAssignmentMatrixEntries
            .FirstOrDefaultAsync(x => x.ParentScopeMatrixId == matrixId, cancellationToken);
}
