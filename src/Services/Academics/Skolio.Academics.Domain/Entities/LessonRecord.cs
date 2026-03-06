using Skolio.Academics.Domain.Exceptions;

namespace Skolio.Academics.Domain.Entities;

public sealed class LessonRecord
{
    private LessonRecord(Guid id, Guid timetableEntryId, DateOnly lessonDate, string topic, string summary)
    {
        Id = id;
        TimetableEntryId = timetableEntryId;
        LessonDate = lessonDate;
        Topic = topic.Trim();
        Summary = summary.Trim();
    }

    public Guid Id { get; }
    public Guid TimetableEntryId { get; }
    public DateOnly LessonDate { get; }
    public string Topic { get; }
    public string Summary { get; }

    public static LessonRecord Create(Guid id, Guid timetableEntryId, DateOnly lessonDate, string topic, string summary)
    {
        if (id == Guid.Empty || timetableEntryId == Guid.Empty)
            throw new AcademicsDomainException("Lesson record ids are required.");
        if (string.IsNullOrWhiteSpace(topic))
            throw new AcademicsDomainException("Lesson topic is required.");

        return new LessonRecord(id, timetableEntryId, lessonDate, topic, summary);
    }
}
