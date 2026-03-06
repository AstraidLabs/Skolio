using Skolio.Academics.Domain.Exceptions;

namespace Skolio.Academics.Domain.Entities;

public sealed class GradeEntry
{
    private GradeEntry(Guid id, Guid studentUserId, Guid subjectId, string gradeValue, string note, DateOnly gradedOn)
    {
        Id = id;
        StudentUserId = studentUserId;
        SubjectId = subjectId;
        GradeValue = gradeValue.Trim();
        Note = note.Trim();
        GradedOn = gradedOn;
    }

    public Guid Id { get; }
    public Guid StudentUserId { get; }
    public Guid SubjectId { get; }
    public string GradeValue { get; }
    public string Note { get; }
    public DateOnly GradedOn { get; }

    public static GradeEntry Create(Guid id, Guid studentUserId, Guid subjectId, string gradeValue, string note, DateOnly gradedOn)
    {
        if (id == Guid.Empty || studentUserId == Guid.Empty || subjectId == Guid.Empty)
            throw new AcademicsDomainException("Grade entry ids are required.");
        if (string.IsNullOrWhiteSpace(gradeValue))
            throw new AcademicsDomainException("Grade value is required.");

        return new GradeEntry(id, studentUserId, subjectId, gradeValue, note, gradedOn);
    }
}
