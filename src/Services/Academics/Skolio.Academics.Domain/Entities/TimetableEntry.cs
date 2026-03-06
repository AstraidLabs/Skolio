using Skolio.Academics.Domain.Enums;
using Skolio.Academics.Domain.Exceptions;

namespace Skolio.Academics.Domain.Entities;

public sealed class TimetableEntry
{
    private TimetableEntry(Guid id, Guid schoolId, Guid schoolYearId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, LessonAudienceType audienceType, Guid audienceId, Guid subjectId, Guid teacherUserId)
    {
        Id = id;
        SchoolId = schoolId;
        SchoolYearId = schoolYearId;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        AudienceType = audienceType;
        AudienceId = audienceId;
        SubjectId = subjectId;
        TeacherUserId = teacherUserId;
    }

    public Guid Id { get; }
    public Guid SchoolId { get; }
    public Guid SchoolYearId { get; }
    public DayOfWeek DayOfWeek { get; }
    public TimeOnly StartTime { get; }
    public TimeOnly EndTime { get; }
    public LessonAudienceType AudienceType { get; }
    public Guid AudienceId { get; }
    public Guid SubjectId { get; }
    public Guid TeacherUserId { get; }

    public static TimetableEntry Create(Guid id, Guid schoolId, Guid schoolYearId, DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, LessonAudienceType audienceType, Guid audienceId, Guid subjectId, Guid teacherUserId)
    {
        if (id == Guid.Empty || schoolId == Guid.Empty || schoolYearId == Guid.Empty || audienceId == Guid.Empty || subjectId == Guid.Empty || teacherUserId == Guid.Empty)
            throw new AcademicsDomainException("Timetable ids are required.");
        if (endTime <= startTime)
            throw new AcademicsDomainException("Lesson end time must be after start time.");

        return new TimetableEntry(id, schoolId, schoolYearId, dayOfWeek, startTime, endTime, audienceType, audienceId, subjectId, teacherUserId);
    }
}
