using Skolio.Academics.Domain.Enums;

namespace Skolio.Academics.Application.Contracts;

public sealed record TimetableEntryContract(Guid Id, Guid SchoolId, Guid SchoolYearId, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, LessonAudienceType AudienceType, Guid AudienceId, Guid SubjectId, Guid TeacherUserId);
public sealed record LessonRecordContract(Guid Id, Guid TimetableEntryId, DateOnly LessonDate, string Topic, string Summary);
public sealed record AttendanceRecordContract(Guid Id, Guid SchoolId, Guid AudienceId, Guid StudentUserId, DateOnly AttendanceDate, AttendanceStatus Status);
public sealed record ExcuseNoteContract(Guid Id, Guid AttendanceRecordId, Guid ParentUserId, string Reason, DateTimeOffset SubmittedAtUtc);
public sealed record GradeEntryContract(Guid Id, Guid StudentUserId, Guid SubjectId, string GradeValue, string Note, DateOnly GradedOn);
public sealed record HomeworkAssignmentContract(Guid Id, Guid SchoolId, Guid AudienceId, Guid SubjectId, string Title, string Instructions, DateOnly DueDate);
public sealed record DailyReportContract(Guid Id, Guid SchoolId, Guid AudienceId, DateOnly ReportDate, string Summary, string Notes);
