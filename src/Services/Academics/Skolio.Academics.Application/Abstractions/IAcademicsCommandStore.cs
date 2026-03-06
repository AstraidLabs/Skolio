using Skolio.Academics.Domain.Entities;

namespace Skolio.Academics.Application.Abstractions;

public interface IAcademicsCommandStore
{
    Task AddTimetableEntryAsync(TimetableEntry entry, CancellationToken cancellationToken);
    Task AddLessonRecordAsync(LessonRecord record, CancellationToken cancellationToken);
    Task AddAttendanceRecordAsync(AttendanceRecord record, CancellationToken cancellationToken);
    Task AddExcuseNoteAsync(ExcuseNote note, CancellationToken cancellationToken);
    Task AddGradeEntryAsync(GradeEntry entry, CancellationToken cancellationToken);
    Task AddHomeworkAssignmentAsync(HomeworkAssignment assignment, CancellationToken cancellationToken);
    Task AddDailyReportAsync(DailyReport report, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
