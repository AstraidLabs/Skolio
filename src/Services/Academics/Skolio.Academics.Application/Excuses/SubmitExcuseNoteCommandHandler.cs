using Mapster;
using MediatR;
using Skolio.Academics.Application.Abstractions;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Domain.Entities;

namespace Skolio.Academics.Application.Excuses;

public sealed class SubmitExcuseNoteCommandHandler(IAcademicsCommandStore commandStore, IAcademicsClock clock, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<SubmitExcuseNoteCommand, ExcuseNoteContract>
{
    public async Task<ExcuseNoteContract> Handle(SubmitExcuseNoteCommand request, CancellationToken cancellationToken)
    {
        var note = ExcuseNote.Create(Guid.NewGuid(), request.AttendanceRecordId, request.ParentUserId, request.Reason, clock.UtcNow);
        await commandStore.AddExcuseNoteAsync(note, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return note.Adapt<ExcuseNoteContract>(mapsterConfig);
    }
}
