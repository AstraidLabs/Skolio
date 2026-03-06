using Mapster;
using MediatR;
using Skolio.Organization.Application.Abstractions;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Application.TeacherAssignments;

public sealed class AssignTeacherCommandHandler(IOrganizationCommandStore commandStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<AssignTeacherCommand, TeacherAssignmentContract>
{
    public async Task<TeacherAssignmentContract> Handle(AssignTeacherCommand request, CancellationToken cancellationToken)
    {
        var assignment = TeacherAssignment.Create(
            Guid.NewGuid(),
            request.SchoolId,
            request.TeacherUserId,
            request.Scope,
            request.ClassRoomId,
            request.TeachingGroupId,
            request.SubjectId);

        await commandStore.AddTeacherAssignmentAsync(assignment, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return assignment.Adapt<TeacherAssignmentContract>(mapsterConfig);
    }
}
