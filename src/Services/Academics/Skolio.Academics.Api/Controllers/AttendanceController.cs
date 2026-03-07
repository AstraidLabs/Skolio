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
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<IReadOnlyCollection<AttendanceRecordContract>>> Records([FromQuery] Guid schoolId, [FromQuery] Guid? audienceId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var query = dbContext.AttendanceRecords.Where(x => x.SchoolId == schoolId);
        if (audienceId.HasValue)
        {
            query = query.Where(x => x.AudienceId == audienceId.Value);
        }

        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            if (!audienceId.HasValue) return BadRequest("Teacher read scope requires audienceId.");

            var hasTeachingContext = await dbContext.TimetableEntries.AnyAsync(x => x.SchoolId == schoolId && x.TeacherUserId == actorUserId && x.AudienceId == audienceId.Value, cancellationToken);
            if (!hasTeachingContext) return Forbid();
        }

        return Ok(await query.OrderByDescending(x => x.AttendanceDate)
            .Select(x => new AttendanceRecordContract(x.Id, x.SchoolId, x.AudienceId, x.StudentUserId, x.AttendanceDate, x.Status))
            .ToListAsync(cancellationToken));
    }

    [HttpPost("records")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<AttendanceRecordContract>> RecordAttendance([FromBody] RecordAttendanceRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();

            var hasTeachingContext = await dbContext.TimetableEntries.AnyAsync(x => x.SchoolId == request.SchoolId && x.TeacherUserId == actorUserId && x.AudienceId == request.AudienceId, cancellationToken);
            if (!hasTeachingContext) return Forbid();
        }

        var result = await mediator.Send(new RecordAttendanceCommand(request.SchoolId, request.AudienceId, request.StudentUserId, request.AttendanceDate, request.Status), cancellationToken);
        Audit("academics.attendance.changed", request.SchoolId, new { operation = "create", result.Id, request.AudienceId, request.StudentUserId, request.AttendanceDate, request.Status });
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

        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();

            var hasTeachingContext = await dbContext.TimetableEntries.AnyAsync(x => x.SchoolId == attendance.SchoolId && x.TeacherUserId == actorUserId && x.AudienceId == attendance.AudienceId, cancellationToken);
            if (!hasTeachingContext) return Forbid();
        }

        var result = await mediator.Send(new SubmitExcuseNoteCommand(request.AttendanceRecordId, request.ParentUserId, request.Reason), cancellationToken);
        Audit("academics.excuse-note.changed", attendance.SchoolId, new { operation = "create", result.Id, request.AttendanceRecordId, request.ParentUserId });
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

    private Guid ResolveActorUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var actorUserId) ? actorUserId : Guid.Empty;
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
