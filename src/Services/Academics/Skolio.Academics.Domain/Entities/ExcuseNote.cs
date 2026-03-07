using Skolio.Academics.Domain.Exceptions;

namespace Skolio.Academics.Domain.Entities;

public sealed class ExcuseNote
{
    private ExcuseNote(Guid id, Guid attendanceRecordId, Guid parentUserId, string reason, DateTimeOffset submittedAtUtc)
    {
        Id = id;
        AttendanceRecordId = attendanceRecordId;
        ParentUserId = parentUserId;
        Reason = reason.Trim();
        SubmittedAtUtc = submittedAtUtc;
    }

    public Guid Id { get; }
    public Guid AttendanceRecordId { get; }
    public Guid ParentUserId { get; }
    public string Reason { get; private set; }
    public DateTimeOffset SubmittedAtUtc { get; private set; }

    public static ExcuseNote Create(Guid id, Guid attendanceRecordId, Guid parentUserId, string reason, DateTimeOffset submittedAtUtc)
    {
        if (id == Guid.Empty || attendanceRecordId == Guid.Empty || parentUserId == Guid.Empty)
            throw new AcademicsDomainException("Excuse note ids are required.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new AcademicsDomainException("Excuse reason is required.");

        return new ExcuseNote(id, attendanceRecordId, parentUserId, reason, submittedAtUtc);
    }

    public void OverrideForPlatformSupport(string reason, DateTimeOffset submittedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new AcademicsDomainException("Excuse reason is required.");

        Reason = reason.Trim();
        SubmittedAtUtc = submittedAtUtc;
    }
}