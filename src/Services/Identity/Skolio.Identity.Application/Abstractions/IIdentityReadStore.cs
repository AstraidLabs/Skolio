using Skolio.Identity.Domain.Entities;

namespace Skolio.Identity.Application.Abstractions;

public interface IIdentityReadStore
{
    Task<UserProfile?> GetUserProfileAsync(Guid userProfileId, CancellationToken cancellationToken);
}
