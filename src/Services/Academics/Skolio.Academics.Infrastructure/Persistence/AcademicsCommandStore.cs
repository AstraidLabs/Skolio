using Skolio.Academics.Application.Abstractions;
using Skolio.Academics.Domain.Entities;

namespace Skolio.Academics.Infrastructure.Persistence;

public sealed class AcademicsCommandStore(AcademicsDbContext dbContext) : IAcademicsCommandStore
{
    public Task AddTimetableEntryAsync(TimetableEntry entry, CancellationToken cancellationToken) => dbContext.TimetableEntries.AddAsync(entry, cancellationToken).AsTask();
    public Task AddLessonRecordAsync(LessonRecord record, CancellationToken cancellationToken) => dbContext.LessonRecords.AddAsync(record, cancellationToken).AsTask();
    public Task AddAttendanceRecordAsync(AttendanceRecord record, CancellationToken cancellationToken) => dbContext.AttendanceRecords.AddAsync(record, cancellationToken).AsTask();
    public Task AddExcuseNoteAsync(ExcuseNote note, CancellationToken cancellationToken) => dbContext.ExcuseNotes.AddAsync(note, cancellationToken).AsTask();
    public Task AddGradeEntryAsync(GradeEntry entry, CancellationToken cancellationToken) => dbContext.GradeEntries.AddAsync(entry, cancellationToken).AsTask();
    public Task AddHomeworkAssignmentAsync(HomeworkAssignment assignment, CancellationToken cancellationToken) => dbContext.HomeworkAssignments.AddAsync(assignment, cancellationToken).AsTask();
    public Task AddDailyReportAsync(DailyReport report, CancellationToken cancellationToken) => dbContext.DailyReports.AddAsync(report, cancellationToken).AsTask();
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
