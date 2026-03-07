using Skolio.Academics.Domain.Enums;
using Skolio.Academics.Domain.Exceptions;

namespace Skolio.Academics.Domain.Entities;

public sealed class AttendanceRecord
{
    private AttendanceRecord(Guid id, Guid schoolId, Guid audienceId, Guid studentUserId, DateOnly attendanceDate, AttendanceStatus status)
    {
        Id = id;
        SchoolId = schoolId;
        AudienceId = audienceId;
        StudentUserId = studentUserId;
        AttendanceDate = attendanceDate;
        Status = status;
    }

    public Guid Id { get; }
    public Guid SchoolId { get; }
    public Guid AudienceId { get; private set; }
    public Guid StudentUserId { get; private set; }
    public DateOnly AttendanceDate { get; private set; }
    public AttendanceStatus Status { get; private set; }

    public static AttendanceRecord Create(Guid id, Guid schoolId, Guid audienceId, Guid studentUserId, DateOnly attendanceDate, AttendanceStatus status)
    {
        if (id == Guid.Empty || schoolId == Guid.Empty || audienceId == Guid.Empty || studentUserId == Guid.Empty)
            throw new AcademicsDomainException("Attendance ids are required.");

        return new AttendanceRecord(id, schoolId, audienceId, studentUserId, attendanceDate, status);
    }

    public void MarkExcused() => Status = AttendanceStatus.Excused;

    public void OverrideForPlatformSupport(Guid audienceId, Guid studentUserId, DateOnly attendanceDate, AttendanceStatus status)
    {
        if (audienceId == Guid.Empty || studentUserId == Guid.Empty)
            throw new AcademicsDomainException("Attendance ids are required.");

        AudienceId = audienceId;
        StudentUserId = studentUserId;
        AttendanceDate = attendanceDate;
        Status = status;
    }
}