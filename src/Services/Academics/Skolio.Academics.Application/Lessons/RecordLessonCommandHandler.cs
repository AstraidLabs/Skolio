using Mapster;
using MediatR;
using Skolio.Academics.Application.Abstractions;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Domain.Entities;

namespace Skolio.Academics.Application.Lessons;

public sealed class RecordLessonCommandHandler(IAcademicsCommandStore commandStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<RecordLessonCommand, LessonRecordContract>
{
    public async Task<LessonRecordContract> Handle(RecordLessonCommand request, CancellationToken cancellationToken)
    {
        var record = LessonRecord.Create(Guid.NewGuid(), request.TimetableEntryId, request.LessonDate, request.Topic, request.Summary);
        await commandStore.AddLessonRecordAsync(record, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return record.Adapt<LessonRecordContract>(mapsterConfig);
    }
}
