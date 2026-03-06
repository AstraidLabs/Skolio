using Mapster;
using MediatR;
using Skolio.Academics.Application.Abstractions;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Domain.Entities;

namespace Skolio.Academics.Application.Timetable;

public sealed class CreateTimetableEntryCommandHandler(IAcademicsCommandStore commandStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<CreateTimetableEntryCommand, TimetableEntryContract>
{
    public async Task<TimetableEntryContract> Handle(CreateTimetableEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = TimetableEntry.Create(Guid.NewGuid(), request.SchoolId, request.SchoolYearId, request.DayOfWeek, request.StartTime, request.EndTime, request.AudienceType, request.AudienceId, request.SubjectId, request.TeacherUserId);
        await commandStore.AddTimetableEntryAsync(entry, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return entry.Adapt<TimetableEntryContract>(mapsterConfig);
    }
}
