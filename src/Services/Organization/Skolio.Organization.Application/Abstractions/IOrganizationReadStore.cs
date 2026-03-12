using Skolio.Organization.Domain.Entities;
using Skolio.Organization.Domain.Enums;

namespace Skolio.Organization.Application.Abstractions;

public interface IOrganizationReadStore
{
    Task<School?> GetSchoolAsync(Guid schoolId, CancellationToken cancellationToken);
    Task<SchoolContextScopeMatrix?> GetSchoolContextMatrixBySchoolTypeAsync(SchoolType schoolType, CancellationToken cancellationToken);
    Task<SchoolScopeOverride?> GetSchoolScopeOverrideAsync(Guid schoolId, CancellationToken cancellationToken);
    Task<bool> SchoolIzoExistsAsync(string schoolIzo, Guid? excludeSchoolId, CancellationToken cancellationToken);
    Task<bool> OperatorIcoExistsAsync(string ico, Guid? excludeOperatorId, CancellationToken cancellationToken);
    Task<bool> OperatorRedIzoExistsAsync(string redIzo, Guid? excludeOperatorId, CancellationToken cancellationToken);
    Task<bool> FounderIcoExistsAsync(string ico, Guid? excludeFounderId, CancellationToken cancellationToken);
}
