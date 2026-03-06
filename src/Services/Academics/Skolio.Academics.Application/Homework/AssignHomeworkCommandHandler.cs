using Mapster;
using MediatR;
using Skolio.Academics.Application.Abstractions;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Domain.Entities;

namespace Skolio.Academics.Application.Homework;

public sealed class AssignHomeworkCommandHandler(IAcademicsCommandStore commandStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<AssignHomeworkCommand, HomeworkAssignmentContract>
{
    public async Task<HomeworkAssignmentContract> Handle(AssignHomeworkCommand request, CancellationToken cancellationToken)
    {
        var assignment = HomeworkAssignment.Create(Guid.NewGuid(), request.SchoolId, request.AudienceId, request.SubjectId, request.Title, request.Instructions, request.DueDate);
        await commandStore.AddHomeworkAssignmentAsync(assignment, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return assignment.Adapt<HomeworkAssignmentContract>(mapsterConfig);
    }
}
