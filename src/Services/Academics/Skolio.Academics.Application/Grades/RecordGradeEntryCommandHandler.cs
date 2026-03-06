using Mapster;
using MediatR;
using Skolio.Academics.Application.Abstractions;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Domain.Entities;

namespace Skolio.Academics.Application.Grades;

public sealed class RecordGradeEntryCommandHandler(IAcademicsCommandStore commandStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<RecordGradeEntryCommand, GradeEntryContract>
{
    public async Task<GradeEntryContract> Handle(RecordGradeEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = GradeEntry.Create(Guid.NewGuid(), request.StudentUserId, request.SubjectId, request.GradeValue, request.Note, request.GradedOn);
        await commandStore.AddGradeEntryAsync(entry, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return entry.Adapt<GradeEntryContract>(mapsterConfig);
    }
}
