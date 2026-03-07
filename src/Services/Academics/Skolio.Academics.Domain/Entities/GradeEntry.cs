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
    public Guid StudentUserId { get; private set; }
    public Guid SubjectId { get; private set; }
    public string GradeValue { get; private set; }
    public string Note { get; private set; }
    public DateOnly GradedOn { get; private set; }

    public static GradeEntry Create(Guid id, Guid studentUserId, Guid subjectId, string gradeValue, string note, DateOnly gradedOn)
    {
        if (id == Guid.Empty || studentUserId == Guid.Empty || subjectId == Guid.Empty)
            throw new AcademicsDomainException("Grade entry ids are required.");
        if (string.IsNullOrWhiteSpace(gradeValue))
            throw new AcademicsDomainException("Grade value is required.");

        return new GradeEntry(id, studentUserId, subjectId, gradeValue, note, gradedOn);
    }

    public void OverrideForPlatformSupport(Guid studentUserId, Guid subjectId, string gradeValue, string note, DateOnly gradedOn)
    {
        if (studentUserId == Guid.Empty || subjectId == Guid.Empty)
            throw new AcademicsDomainException("Grade entry ids are required.");
        if (string.IsNullOrWhiteSpace(gradeValue))
            throw new AcademicsDomainException("Grade value is required.");

        StudentUserId = studentUserId;
        SubjectId = subjectId;
        GradeValue = gradeValue.Trim();
        Note = note.Trim();
        GradedOn = gradedOn;
    }
}