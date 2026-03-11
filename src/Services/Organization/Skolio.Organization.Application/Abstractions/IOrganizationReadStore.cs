using Skolio.Organization.Domain.Entities;
using Skolio.Organization.Domain.Enums;

namespace Skolio.Organization.Application.Abstractions;

public interface IOrganizationReadStore
{
    Task<School?> GetSchoolAsync(Guid schoolId, CancellationToken cancellationToken);
    Task<SchoolContextScopeMatrix?> GetSchoolContextMatrixBySchoolTypeAsync(SchoolType schoolType, CancellationToken cancellationToken);
    Task<SchoolScopeOverride?> GetSchoolScopeOverrideAsync(Guid schoolId, CancellationToken cancellationToken);
}
