using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Application.Abstractions;

public interface IOrganizationReadStore
{
    Task<School?> GetSchoolAsync(Guid schoolId, CancellationToken cancellationToken);
}
