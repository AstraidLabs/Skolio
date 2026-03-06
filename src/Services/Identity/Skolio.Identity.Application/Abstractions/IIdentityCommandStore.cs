using Skolio.Identity.Domain.Entities;

namespace Skolio.Identity.Application.Abstractions;

public interface IIdentityCommandStore
{
    Task UpsertUserProfileAsync(UserProfile userProfile, CancellationToken cancellationToken);
    Task AddSchoolRoleAssignmentAsync(SchoolRoleAssignment assignment, CancellationToken cancellationToken);
    Task AddParentStudentLinkAsync(ParentStudentLink link, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
