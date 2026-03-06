using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Application.Abstractions;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence;

public sealed class OrganizationReadStore(OrganizationDbContext dbContext) : IOrganizationReadStore
{
    public Task<School?> GetSchoolAsync(Guid schoolId, CancellationToken cancellationToken)
        => dbContext.Schools.FirstOrDefaultAsync(x => x.Id == schoolId, cancellationToken);
}
