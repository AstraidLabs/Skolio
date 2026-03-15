using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.ServiceDefaults.Authorization;
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
    private static readonly TimeSpan ParentExcuseUpdateWindow = TimeSpan.FromHours(48);

    [HttpGet("records")]
    [Authorize(Policy = SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<AttendanceRecordContract>>> Records([FromQuery] Guid schoolId, [FromQuery] Guid? audienceId, [FromQuery] Guid? studentUserId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var query = dbContext.AttendanceRecords.Where(x => x.SchoolId == schoolId);
        if (audienceId.HasValue)
        {
            query = query.Where(x => x.AudienceId == audienceId.Value);
        }

        if (studentUserId.HasValue)
        {
            query = query.Where(x => x.StudentUserId == studentUserId.Value);
        }

        if (IsParentOnly())
        {
            if (!studentUserId.HasValue) return this.ValidationField("studentUserId", "Parent read scope requires studentUserId.");
            if (!SchoolScope.GetLinkedStudentIds(User).Contains(studentUserId.Value)) return Forbid();
        }
        else if (IsStudentOnly())
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            if (studentUserId.HasValue && studentUserId.Value != actorUserId) return Forbid();
            query = query.Where(x => x.StudentUserId == actorUserId);
        }
        else if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            if (!audienceId.HasValue) return this.ValidationField("audienceId", "Teacher read scope requires audienceId.");

            var hasTeachingContext = await dbContext.TimetableEntries.AnyAsync(x => x.SchoolId == schoolId && x.TeacherUserId == actorUserId && x.AudienceId == audienceId.Value, cancellationToken);
            if (!hasTeachingContext) return Forbid();
        }

        return Ok(await query.OrderByDescending(x => x.AttendanceDate)
            .Select(x => new AttendanceRecordContract(x.Id, x.SchoolId, x.AudienceId, x.StudentUserId, x.AttendanceDate, x.Status))
            .ToListAsync(cancellationToken));
    }

    [HttpPost("records")]
    [Authorize(Policy = SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
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
    [Authorize(Policy = SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<AttendanceRecordContract>> OverrideAttendance(Guid id, [FromBody] OverrideAttendanceRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return this.ValidationField("overrideReason", "Override reason is required.");

        var entity = await dbContext.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.OverrideForPlatformSupport(request.AudienceId, request.StudentUserId, request.AttendanceDate, request.Status);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("academics.attendance.override", entity.SchoolId, new { request.OverrideReason, entity.Id, request.Status });
        return Ok(new AttendanceRecordContract(entity.Id, entity.SchoolId, entity.AudienceId, entity.StudentUserId, entity.AttendanceDate, entity.Status));
    }

    [HttpGet("excuse-notes")]
    [Authorize(Policy = SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<ExcuseNoteContract>>> ExcuseNotes([FromQuery] Guid schoolId, [FromQuery] Guid? studentUserId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var query = dbContext.ExcuseNotes
            .Join(dbContext.AttendanceRecords, excuse => excuse.AttendanceRecordId, attendance => attendance.Id, (excuse, attendance) => new { excuse, attendance })
            .Where(x => x.attendance.SchoolId == schoolId);

        if (studentUserId.HasValue)
        {
            query = query.Where(x => x.attendance.StudentUserId == studentUserId.Value);
        }

        if (IsParentOnly())
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            query = query.Where(x => x.excuse.ParentUserId == actorUserId);

            if (studentUserId.HasValue && !SchoolScope.GetLinkedStudentIds(User).Contains(studentUserId.Value)) return Forbid();
        }
        else if (IsStudentOnly())
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            if (studentUserId.HasValue && studentUserId.Value != actorUserId) return Forbid();
            query = query.Where(x => x.attendance.StudentUserId == actorUserId);
        }

        var result = await query
            .OrderByDescending(x => x.excuse.SubmittedAtUtc)
            .Select(x => new ExcuseNoteContract(x.excuse.Id, x.excuse.AttendanceRecordId, x.excuse.ParentUserId, x.excuse.Reason, x.excuse.SubmittedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("my/excuse-requests")]
    [Authorize(Policy = SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<ExcuseNoteContract>>> MyExcuseRequests(CancellationToken cancellationToken)
    {
        if (!IsParentOnly()) return Forbid();

        var actorUserId = ResolveActorUserId();
        if (actorUserId == Guid.Empty) return Forbid();

        var linkedStudentIds = SchoolScope.GetLinkedStudentIds(User);
        if (linkedStudentIds.Count == 0) return Ok(Array.Empty<ExcuseNoteContract>());

        var result = await dbContext.ExcuseNotes
            .Join(dbContext.AttendanceRecords, excuse => excuse.AttendanceRecordId, attendance => attendance.Id, (excuse, attendance) => new { excuse, attendance })
            .Where(x => x.excuse.ParentUserId == actorUserId && linkedStudentIds.Contains(x.attendance.StudentUserId))
            .OrderByDescending(x => x.excuse.SubmittedAtUtc)
            .Select(x => new ExcuseNoteContract(x.excuse.Id, x.excuse.AttendanceRecordId, x.excuse.ParentUserId, x.excuse.Reason, x.excuse.SubmittedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost("my/excuse-requests")]
    [Authorize(Policy = SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<ExcuseNoteContract>> SubmitMyExcuse([FromBody] SubmitMyExcuseNoteRequest request, CancellationToken cancellationToken)
    {
        if (!IsParentOnly()) return Forbid();

        var actorUserId = ResolveActorUserId();
        if (actorUserId == Guid.Empty) return Forbid();

        var attendance = await dbContext.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == request.AttendanceRecordId, cancellationToken);
        if (attendance is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, attendance.SchoolId)) return Forbid();
        if (!SchoolScope.GetLinkedStudentIds(User).Contains(attendance.StudentUserId)) return Forbid();

        var result = await mediator.Send(new SubmitExcuseNoteCommand(request.AttendanceRecordId, actorUserId, request.Reason), cancellationToken);
        Audit("academics.excuse-note.changed", attendance.SchoolId, new { operation = "create", result.Id, request.AttendanceRecordId, parentUserId = actorUserId, selfService = true });
        return CreatedAtAction(nameof(SubmitMyExcuse), new { id = result.Id }, result);
    }

    [HttpPut("my/excuse-requests/{id:guid}")]
    [Authorize(Policy = SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<ExcuseNoteContract>> UpdateMyExcuse(Guid id, [FromBody] UpdateExcuseRequest request, CancellationToken cancellationToken)
    {
        if (!IsParentOnly()) return Forbid();

        var actorUserId = ResolveActorUserId();
        if (actorUserId == Guid.Empty) return Forbid();

        var entity = await dbContext.ExcuseNotes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        if (entity.ParentUserId != actorUserId) return Forbid();

        var attendance = await dbContext.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == entity.AttendanceRecordId, cancellationToken);
        if (attendance is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, attendance.SchoolId)) return Forbid();
        if (!SchoolScope.GetLinkedStudentIds(User).Contains(attendance.StudentUserId)) return Forbid();
        if (DateTimeOffset.UtcNow - entity.SubmittedAtUtc > ParentExcuseUpdateWindow) return this.ValidationForm("Excuse update window expired.");

        entity.UpdateByParent(request.Reason, DateTimeOffset.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("academics.excuse-note.changed", attendance.SchoolId, new { operation = "update", entity.Id, parentUserId = actorUserId, selfService = true });
        return Ok(new ExcuseNoteContract(entity.Id, entity.AttendanceRecordId, entity.ParentUserId, entity.Reason, entity.SubmittedAtUtc));
    }

    [HttpDelete("my/excuse-requests/{id:guid}")]
    [Authorize(Policy = SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<IActionResult> CancelMyExcuse(Guid id, CancellationToken cancellationToken)
    {
        if (!IsParentOnly()) return Forbid();

        var actorUserId = ResolveActorUserId();
        if (actorUserId == Guid.Empty) return Forbid();

        var entity = await dbContext.ExcuseNotes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        if (entity.ParentUserId != actorUserId) return Forbid();

        var attendance = await dbContext.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == entity.AttendanceRecordId, cancellationToken);
        if (attendance is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, attendance.SchoolId)) return Forbid();
        if (!SchoolScope.GetLinkedStudentIds(User).Contains(attendance.StudentUserId)) return Forbid();
        if (DateTimeOffset.UtcNow - entity.SubmittedAtUtc > ParentExcuseUpdateWindow) return this.ValidationForm("Excuse cancellation window expired.");

        dbContext.ExcuseNotes.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("academics.excuse-note.changed", attendance.SchoolId, new { operation = "cancel", entity.Id, parentUserId = actorUserId, selfService = true });
        return NoContent();
    }

    [HttpPost("excuse-notes")]
    [Authorize(Policy = SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<ExcuseNoteContract>> SubmitExcuse([FromBody] SubmitExcuseNoteRequest request, CancellationToken cancellationToken)
    {
        var attendance = await dbContext.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == request.AttendanceRecordId, cancellationToken);
        if (attendance is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, attendance.SchoolId)) return Forbid();

        if (IsParentOnly())
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty || request.ParentUserId != actorUserId) return Forbid();
            if (!SchoolScope.GetLinkedStudentIds(User).Contains(attendance.StudentUserId)) return Forbid();
        }
        else if (IsStudentOnly())
        {
            return Forbid();
        }
        else if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
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

    [HttpPut("excuse-notes/{id:guid}")]
    [Authorize(Policy = SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<ExcuseNoteContract>> UpdateExcuse(Guid id, [FromBody] UpdateExcuseRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ExcuseNotes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        var attendance = await dbContext.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == entity.AttendanceRecordId, cancellationToken);
        if (attendance is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, attendance.SchoolId)) return Forbid();

        if (IsParentOnly())
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty || entity.ParentUserId != actorUserId) return Forbid();
            if (!SchoolScope.GetLinkedStudentIds(User).Contains(attendance.StudentUserId)) return Forbid();
            if (DateTimeOffset.UtcNow - entity.SubmittedAtUtc > ParentExcuseUpdateWindow) return this.ValidationForm("Excuse update window expired.");

            entity.UpdateByParent(request.Reason, DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
            Audit("academics.excuse-note.changed", attendance.SchoolId, new { operation = "update", entity.Id });
            return Ok(new ExcuseNoteContract(entity.Id, entity.AttendanceRecordId, entity.ParentUserId, entity.Reason, entity.SubmittedAtUtc));
        }
        else if (IsStudentOnly())
        {
            return Forbid();
        }

        return Forbid();
    }

    [HttpDelete("excuse-notes/{id:guid}")]
    [Authorize(Policy = SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<IActionResult> CancelExcuse(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ExcuseNotes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        var attendance = await dbContext.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == entity.AttendanceRecordId, cancellationToken);
        if (attendance is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, attendance.SchoolId)) return Forbid();

        if (IsParentOnly())
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty || entity.ParentUserId != actorUserId) return Forbid();
            if (!SchoolScope.GetLinkedStudentIds(User).Contains(attendance.StudentUserId)) return Forbid();
            if (DateTimeOffset.UtcNow - entity.SubmittedAtUtc > ParentExcuseUpdateWindow) return this.ValidationForm("Excuse cancellation window expired.");

            dbContext.ExcuseNotes.Remove(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
            Audit("academics.excuse-note.changed", attendance.SchoolId, new { operation = "cancel", entity.Id });
            return NoContent();
        }
        else if (IsStudentOnly())
        {
            return Forbid();
        }

        return Forbid();
    }

    [HttpPut("excuse-notes/{id:guid}/override")]
    [Authorize(Policy = SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<ExcuseNoteContract>> OverrideExcuse(Guid id, [FromBody] OverrideExcuseRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return this.ValidationField("overrideReason", "Override reason is required.");

        var entity = await dbContext.ExcuseNotes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.OverrideForPlatformSupport(request.Reason, request.SubmittedAtUtc);
        await dbContext.SaveChangesAsync(cancellationToken);

        var attendance = await dbContext.AttendanceRecords.FirstOrDefaultAsync(x => x.Id == entity.AttendanceRecordId, cancellationToken);
        Audit("academics.excuse-note.override", attendance?.SchoolId ?? Guid.Empty, new { request.OverrideReason, entity.Id });
        return Ok(new ExcuseNoteContract(entity.Id, entity.AttendanceRecordId, entity.ParentUserId, entity.Reason, entity.SubmittedAtUtc));
    }

    private bool IsParentOnly()
        => User.IsInRole("Parent") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher");

    private bool IsStudentOnly()
        => User.IsInRole("Student") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher") && !User.IsInRole("Parent");

    private Guid ResolveActorUserId() => SchoolScope.ResolveActorUserId(User);

    private void Audit(string actionCode, Guid schoolId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} payload={Payload}", actionCode, actor, schoolId, payload);
    }

    public sealed record RecordAttendanceRequest(Guid SchoolId, Guid AudienceId, Guid StudentUserId, DateOnly AttendanceDate, AttendanceStatus Status);
    public sealed record OverrideAttendanceRequest(Guid AudienceId, Guid StudentUserId, DateOnly AttendanceDate, AttendanceStatus Status, string OverrideReason);
    public sealed record SubmitExcuseNoteRequest(Guid AttendanceRecordId, Guid ParentUserId, string Reason);
    public sealed record SubmitMyExcuseNoteRequest(Guid AttendanceRecordId, string Reason);
    public sealed record UpdateExcuseRequest(string Reason);
    public sealed record OverrideExcuseRequest(string Reason, DateTimeOffset SubmittedAtUtc, string OverrideReason);
}

