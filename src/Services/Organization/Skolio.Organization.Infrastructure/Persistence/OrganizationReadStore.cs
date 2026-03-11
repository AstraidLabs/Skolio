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
}
