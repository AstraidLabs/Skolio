using MediatR;
using Skolio.Academics.Application.Contracts;

namespace Skolio.Academics.Application.Lessons;

public sealed record RecordLessonCommand(Guid TimetableEntryId, DateOnly LessonDate, string Topic, string Summary) : IRequest<LessonRecordContract>;
