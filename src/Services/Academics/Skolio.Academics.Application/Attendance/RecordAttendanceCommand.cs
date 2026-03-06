using MediatR;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Domain.Enums;

namespace Skolio.Academics.Application.Attendance;

public sealed record RecordAttendanceCommand(Guid SchoolId, Guid AudienceId, Guid StudentUserId, DateOnly AttendanceDate, AttendanceStatus Status) : IRequest<AttendanceRecordContract>;
