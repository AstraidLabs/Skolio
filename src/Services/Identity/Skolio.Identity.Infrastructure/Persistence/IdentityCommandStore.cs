using Skolio.Identity.Application.Abstractions;
using Skolio.Identity.Domain.Entities;

namespace Skolio.Identity.Infrastructure.Persistence;

public sealed class IdentityCommandStore(IdentityDbContext dbContext) : IIdentityCommandStore
{
    public Task UpsertUserProfileAsync(UserProfile userProfile, CancellationToken cancellationToken)
    {
        dbContext.UserProfiles.Update(userProfile);
        return Task.CompletedTask;
    }

    public Task AddSchoolRoleAssignmentAsync(SchoolRoleAssignment assignment, CancellationToken cancellationToken) => dbContext.SchoolRoleAssignments.AddAsync(assignment, cancellationToken).AsTask();
    public Task AddParentStudentLinkAsync(ParentStudentLink link, CancellationToken cancellationToken) => dbContext.ParentStudentLinks.AddAsync(link, cancellationToken).AsTask();
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
