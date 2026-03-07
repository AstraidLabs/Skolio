using Skolio.Academics.Domain.Exceptions;

namespace Skolio.Academics.Domain.Entities;

public sealed class HomeworkAssignment
{
    private HomeworkAssignment(Guid id, Guid schoolId, Guid audienceId, Guid subjectId, string title, string instructions, DateOnly dueDate)
    {
        Id = id;
        SchoolId = schoolId;
        AudienceId = audienceId;
        SubjectId = subjectId;
        Title = title.Trim();
        Instructions = instructions.Trim();
        DueDate = dueDate;
    }

    public Guid Id { get; }
    public Guid SchoolId { get; }
    public Guid AudienceId { get; private set; }
    public Guid SubjectId { get; private set; }
    public string Title { get; private set; }
    public string Instructions { get; private set; }
    public DateOnly DueDate { get; private set; }

    public static HomeworkAssignment Create(Guid id, Guid schoolId, Guid audienceId, Guid subjectId, string title, string instructions, DateOnly dueDate)
    {
        if (id == Guid.Empty || schoolId == Guid.Empty || audienceId == Guid.Empty || subjectId == Guid.Empty)
            throw new AcademicsDomainException("Homework ids are required.");
        if (string.IsNullOrWhiteSpace(title))
            throw new AcademicsDomainException("Homework title is required.");

        return new HomeworkAssignment(id, schoolId, audienceId, subjectId, title, instructions, dueDate);
    }

    public void OverrideForPlatformSupport(Guid audienceId, Guid subjectId, string title, string instructions, DateOnly dueDate)
    {
        if (audienceId == Guid.Empty || subjectId == Guid.Empty)
            throw new AcademicsDomainException("Homework ids are required.");
        if (string.IsNullOrWhiteSpace(title))
            throw new AcademicsDomainException("Homework title is required.");

        AudienceId = audienceId;
        SubjectId = subjectId;
        Title = title.Trim();
        Instructions = instructions.Trim();
        DueDate = dueDate;
    }
}