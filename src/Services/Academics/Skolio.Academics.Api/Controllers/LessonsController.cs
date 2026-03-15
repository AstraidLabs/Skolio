using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.Lessons;
using Skolio.Academics.Infrastructure.Persistence;

namespace Skolio.Academics.Api.Controllers;

[ApiController]
[Route("api/academics/lessons")]
public sealed class LessonsController(IMediator mediator, AcademicsDbContext dbContext, ILogger<LessonsController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<LessonRecordContract>>> List([FromQuery] Guid schoolId, [FromQuery] Guid? timetableEntryId, [FromQuery] Guid? studentUserId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var query = dbContext.LessonRecords.Join(dbContext.TimetableEntries, lesson => lesson.TimetableEntryId, timetable => timetable.Id, (lesson, timetable) => new { lesson, timetable })
            .Where(x => x.timetable.SchoolId == schoolId);

        if (timetableEntryId.HasValue)
        {
            query = query.Where(x => x.timetable.Id == timetableEntryId.Value);
        }

        if (IsParentOnly())
        {
            if (!studentUserId.HasValue) return this.ValidationField("studentUserId", "Parent read scope requires studentUserId.");
            if (!SchoolScope.GetLinkedStudentIds(User).Contains(studentUserId.Value)) return Forbid();

            var linkedAudienceIds = await dbContext.AttendanceRecords
                .Where(x => x.SchoolId == schoolId && x.StudentUserId == studentUserId.Value)
                .Select(x => x.AudienceId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(x => linkedAudienceIds.Contains(x.timetable.AudienceId));
        }
        else if (IsStudentOnly())
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            if (studentUserId.HasValue && studentUserId.Value != actorUserId) return Forbid();

            var linkedAudienceIds = await dbContext.AttendanceRecords
                .Where(x => x.SchoolId == schoolId && x.StudentUserId == actorUserId)
                .Select(x => x.AudienceId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(x => linkedAudienceIds.Contains(x.timetable.AudienceId));
        }
        else if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            query = query.Where(x => x.timetable.TeacherUserId == actorUserId);
        }

        return Ok(await query.OrderByDescending(x => x.lesson.LessonDate)
            .Select(x => new LessonRecordContract(x.lesson.Id, x.lesson.TimetableEntryId, x.lesson.LessonDate, x.lesson.Topic, x.lesson.Summary))
            .ToListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<LessonRecordContract>> Record([FromBody] RecordLessonRequest request, CancellationToken cancellationToken)
    {
        var timetable = await dbContext.TimetableEntries.FirstOrDefaultAsync(x => x.Id == request.TimetableEntryId, cancellationToken);
        if (timetable is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, timetable.SchoolId)) return Forbid();

        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty || timetable.TeacherUserId != actorUserId) return Forbid();
        }

        var result = await mediator.Send(new RecordLessonCommand(request.TimetableEntryId, request.LessonDate, request.Topic, request.Summary), cancellationToken);
        Audit("academics.lesson-record.changed", timetable.SchoolId, new { operation = "create", result.Id, request.TimetableEntryId, request.LessonDate, request.Topic });
        return CreatedAtAction(nameof(Record), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/override")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<LessonRecordContract>> OverrideLesson(Guid id, [FromBody] OverrideLessonRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return this.ValidationField("overrideReason", "Override reason is required.");

        var entity = await dbContext.LessonRecords.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.OverrideForPlatformSupport(request.TimetableEntryId, request.LessonDate, request.Topic, request.Summary);
        await dbContext.SaveChangesAsync(cancellationToken);

        var schoolId = await dbContext.TimetableEntries.Where(x => x.Id == request.TimetableEntryId).Select(x => x.SchoolId).FirstOrDefaultAsync(cancellationToken);
        Audit("academics.lesson.override", schoolId, new { request.OverrideReason, entity.Id, request.Topic });
        return Ok(new LessonRecordContract(entity.Id, entity.TimetableEntryId, entity.LessonDate, entity.Topic, entity.Summary));
    }

    private bool IsParentOnly()
        => User.IsInRole("Parent") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher");

    private bool IsStudentOnly()
        => User.IsInRole("Student") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher") && !User.IsInRole("Parent");

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

    public sealed record RecordLessonRequest(Guid TimetableEntryId, DateOnly LessonDate, string Topic, string Summary);
    public sealed record OverrideLessonRequest(Guid TimetableEntryId, DateOnly LessonDate, string Topic, string Summary, string OverrideReason);
}

