using Microsoft.EntityFrameworkCore;
using Skolio.Identity.Application.Abstractions;
using Skolio.Identity.Domain.Entities;

namespace Skolio.Identity.Infrastructure.Persistence;

public sealed class IdentityReadStore(IdentityDbContext dbContext) : IIdentityReadStore
{
    public Task<UserProfile?> GetUserProfileAsync(Guid userProfileId, CancellationToken cancellationToken)
        => dbContext.UserProfiles.FirstOrDefaultAsync(x => x.Id == userProfileId, cancellationToken);
}
