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
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<IReadOnlyCollection<LessonRecordContract>>> List([FromQuery] Guid schoolId, CancellationToken cancellationToken)
        => Ok(await dbContext.LessonRecords.Join(dbContext.TimetableEntries, lesson => lesson.TimetableEntryId, timetable => timetable.Id, (lesson, timetable) => new { lesson, timetable }).Where(x => x.timetable.SchoolId == schoolId).OrderByDescending(x => x.lesson.LessonDate).Select(x => new LessonRecordContract(x.lesson.Id, x.lesson.TimetableEntryId, x.lesson.LessonDate, x.lesson.Topic, x.lesson.Summary)).ToListAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<LessonRecordContract>> Record([FromBody] RecordLessonRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RecordLessonCommand(request.TimetableEntryId, request.LessonDate, request.Topic, request.Summary), cancellationToken);
        return CreatedAtAction(nameof(Record), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/override")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<LessonRecordContract>> OverrideLesson(Guid id, [FromBody] OverrideLessonRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return BadRequest("Override reason is required.");

        var entity = await dbContext.LessonRecords.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.OverrideForPlatformSupport(request.TimetableEntryId, request.LessonDate, request.Topic, request.Summary);
        await dbContext.SaveChangesAsync(cancellationToken);

        var schoolId = await dbContext.TimetableEntries.Where(x => x.Id == request.TimetableEntryId).Select(x => x.SchoolId).FirstOrDefaultAsync(cancellationToken);
        Audit("academics.lesson.override", schoolId, request.OverrideReason, new { entity.Id, request.Topic });
        return Ok(new LessonRecordContract(entity.Id, entity.TimetableEntryId, entity.LessonDate, entity.Topic, entity.Summary));
    }

    private void Audit(string actionCode, Guid schoolId, string overrideReason, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} overrideReason={OverrideReason} payload={Payload}", actionCode, actor, schoolId, overrideReason, payload);
    }

    public sealed record RecordLessonRequest(Guid TimetableEntryId, DateOnly LessonDate, string Topic, string Summary);
    public sealed record OverrideLessonRequest(Guid TimetableEntryId, DateOnly LessonDate, string Topic, string Summary, string OverrideReason);
}