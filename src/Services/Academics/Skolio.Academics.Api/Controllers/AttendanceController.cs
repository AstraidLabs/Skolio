using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Academics.Application.Attendance;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.Excuses;
using Skolio.Academics.Domain.Enums;
using Skolio.Academics.Infrastructure.Persistence;

namespace Skolio.Academics.Api.Controllers;
[ApiController]
[Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministration)]
[Route("api/academics/attendance")]
public sealed class AttendanceController(IMediator mediator, AcademicsDbContext dbContext) : ControllerBase
{
    [HttpGet("records")]
    public async Task<ActionResult<IReadOnlyCollection<AttendanceRecordContract>>> Records([FromQuery] Guid schoolId, CancellationToken cancellationToken)
        => Ok(await dbContext.AttendanceRecords.Where(x => x.SchoolId == schoolId).OrderByDescending(x => x.AttendanceDate).Select(x => new AttendanceRecordContract(x.Id, x.SchoolId, x.AudienceId, x.StudentUserId, x.AttendanceDate, x.Status)).ToListAsync(cancellationToken));

    [HttpPost("records")]
    public async Task<ActionResult<AttendanceRecordContract>> RecordAttendance([FromBody] RecordAttendanceRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RecordAttendanceCommand(request.SchoolId, request.AudienceId, request.StudentUserId, request.AttendanceDate, request.Status), cancellationToken);
        return CreatedAtAction(nameof(RecordAttendance), new { id = result.Id }, result);
    }

    [HttpPost("excuse-notes")]
    public async Task<ActionResult<ExcuseNoteContract>> SubmitExcuse([FromBody] SubmitExcuseNoteRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SubmitExcuseNoteCommand(request.AttendanceRecordId, request.ParentUserId, request.Reason), cancellationToken);
        return CreatedAtAction(nameof(SubmitExcuse), new { id = result.Id }, result);
    }

    public sealed record RecordAttendanceRequest(Guid SchoolId, Guid AudienceId, Guid StudentUserId, DateOnly AttendanceDate, AttendanceStatus Status);
    public sealed record SubmitExcuseNoteRequest(Guid AttendanceRecordId, Guid ParentUserId, string Reason);
}
