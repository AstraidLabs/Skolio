using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Academics.Api.Auth;
using Skolio.Academics.Application.Attendance;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.Excuses;
using Skolio.Academics.Domain.Enums;
using Skolio.Academics.Infrastructure.Persistence;

namespace Skolio.Academics.Api.Controllers;

[ApiController]
[Route("api/academics/attendance")]
public sealed class AttendanceController(IMediator mediator, AcademicsDbContext dbContext, ILogger<AttendanceController> logger) : ControllerBase
{
    [HttpGet("records")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<IReadOnlyCollection<AttendanceRecordContract>>> Records([FromQuery] Guid schoolId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        return Ok(await dbContext.AttendanceRecords.Where(x => x.SchoolId == schoolId).OrderByDescending(x => x.AttendanceDate).Select(x => new AttendanceRecordContract(x.Id, x.SchoolId, x.AudienceId, x.StudentUserId, x.AttendanceDate, x.Status)).ToListAsync(cancellationToken));
    }

    [HttpPost("records")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<AttendanceRecordContract>> RecordAttendance([FromBody] RecordAttendanceRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        var result = await mediator.Send(new RecordAttendanceCommand(request.SchoolId, request.AudienceId, request.StudentUserId, request.AttendanceDate, request.Status), cancellationToken);
        return CreatedAtAction(nameof(RecordAttendance), new { id = result.Id }, result);
    }

    [HttpPut("records/{id:guid}/override")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<AttendanceRecordContract>> OverrideAttendance(Guid id, [FromBody] OverrideAttendanceRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return BadRequest("Override reason is required.");

        var entity = await dbContext.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.OverrideForPlatformSupport(request.AudienceId, request.StudentUserId, request.AttendanceDate, request.Status);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("academics.attendance.override", entity.SchoolId, new { request.OverrideReason, entity.Id, request.Status });
        return Ok(new AttendanceRecordContract(entity.Id, entity.SchoolId, entity.AudienceId, entity.StudentUserId, entity.AttendanceDate, entity.Status));
    }

    [HttpPost("excuse-notes")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<ExcuseNoteContract>> SubmitExcuse([FromBody] SubmitExcuseNoteRequest request, CancellationToken cancellationToken)
    {
        var attendance = await dbContext.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == request.AttendanceRecordId, cancellationToken);
        if (attendance is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, attendance.SchoolId)) return Forbid();

        var result = await mediator.Send(new SubmitExcuseNoteCommand(request.AttendanceRecordId, request.ParentUserId, request.Reason), cancellationToken);
        return CreatedAtAction(nameof(SubmitExcuse), new { id = result.Id }, result);
    }

    [HttpPut("excuse-notes/{id:guid}/override")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<ExcuseNoteContract>> OverrideExcuse(Guid id, [FromBody] OverrideExcuseRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return BadRequest("Override reason is required.");

        var entity = await dbContext.ExcuseNotes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.OverrideForPlatformSupport(request.Reason, request.SubmittedAtUtc);
        await dbContext.SaveChangesAsync(cancellationToken);

        var attendance = await dbContext.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == entity.AttendanceRecordId, cancellationToken);
        Audit("academics.excuse-note.override", attendance?.SchoolId ?? Guid.Empty, new { request.OverrideReason, entity.Id });
        return Ok(new ExcuseNoteContract(entity.Id, entity.AttendanceRecordId, entity.ParentUserId, entity.Reason, entity.SubmittedAtUtc));
    }

    private void Audit(string actionCode, Guid schoolId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} payload={Payload}", actionCode, actor, schoolId, payload);
    }

    public sealed record RecordAttendanceRequest(Guid SchoolId, Guid AudienceId, Guid StudentUserId, DateOnly AttendanceDate, AttendanceStatus Status);
    public sealed record OverrideAttendanceRequest(Guid AudienceId, Guid StudentUserId, DateOnly AttendanceDate, AttendanceStatus Status, string OverrideReason);
    public sealed record SubmitExcuseNoteRequest(Guid AttendanceRecordId, Guid ParentUserId, string Reason);
    public sealed record OverrideExcuseRequest(string Reason, DateTimeOffset SubmittedAtUtc, string OverrideReason);
}
