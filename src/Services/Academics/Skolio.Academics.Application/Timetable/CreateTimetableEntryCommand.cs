using MediatR;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Domain.Enums;

namespace Skolio.Academics.Application.Timetable;

public sealed record CreateTimetableEntryCommand(Guid SchoolId, Guid SchoolYearId, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, LessonAudienceType AudienceType, Guid AudienceId, Guid SubjectId, Guid TeacherUserId) : IRequest<TimetableEntryContract>;
