using Mapster;
using MediatR;
using Skolio.Identity.Application.Abstractions;
using Skolio.Identity.Application.Contracts;
using Skolio.Identity.Domain.Entities;

namespace Skolio.Identity.Application.Roles;

public sealed class AssignSchoolRoleCommandHandler(IIdentityCommandStore commandStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<AssignSchoolRoleCommand, SchoolRoleAssignmentContract>
{
    public async Task<SchoolRoleAssignmentContract> Handle(AssignSchoolRoleCommand request, CancellationToken cancellationToken)
    {
        var assignment = SchoolRoleAssignment.Create(Guid.NewGuid(), request.UserProfileId, request.SchoolId, request.RoleCode);
        await commandStore.AddSchoolRoleAssignmentAsync(assignment, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return assignment.Adapt<SchoolRoleAssignmentContract>(mapsterConfig);
    }
}
