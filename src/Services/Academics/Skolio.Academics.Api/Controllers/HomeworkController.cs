using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.Homework;
using Skolio.Academics.Infrastructure.Persistence;

namespace Skolio.Academics.Api.Controllers;

[ApiController]
[Route("api/academics/homework")]
public sealed class HomeworkController(IMediator mediator, AcademicsDbContext dbContext, ILogger<HomeworkController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<HomeworkAssignmentContract>>> List([FromQuery] Guid schoolId, [FromQuery] Guid? audienceId, [FromQuery] Guid? studentUserId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var query = dbContext.HomeworkAssignments.Where(x => x.SchoolId == schoolId);
        if (audienceId.HasValue)
        {
            query = query.Where(x => x.AudienceId == audienceId.Value);
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

            query = query.Where(x => linkedAudienceIds.Contains(x.AudienceId));
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

            query = query.Where(x => linkedAudienceIds.Contains(x.AudienceId));
        }
        else if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            if (!audienceId.HasValue) return this.ValidationField("audienceId", "Teacher read scope requires audienceId.");

            var hasTeachingContext = await dbContext.TimetableEntries.AnyAsync(x => x.SchoolId == schoolId && x.TeacherUserId == actorUserId && x.AudienceId == audienceId.Value, cancellationToken);
            if (!hasTeachingContext) return Forbid();
        }

        return Ok(await query.OrderBy(x => x.DueDate).Select(x => new HomeworkAssignmentContract(x.Id, x.SchoolId, x.AudienceId, x.SubjectId, x.Title, x.Instructions, x.DueDate)).ToListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<HomeworkAssignmentContract>> Assign([FromBody] AssignHomeworkRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();

            var hasTeachingContext = await dbContext.TimetableEntries.AnyAsync(x => x.SchoolId == request.SchoolId && x.TeacherUserId == actorUserId && x.AudienceId == request.AudienceId && x.SubjectId == request.SubjectId, cancellationToken);
            if (!hasTeachingContext) return Forbid();
        }

        var result = await mediator.Send(new AssignHomeworkCommand(request.SchoolId, request.AudienceId, request.SubjectId, request.Title, request.Instructions, request.DueDate), cancellationToken);
        Audit("academics.homework.changed", request.SchoolId, new { operation = "create", result.Id, request.AudienceId, request.SubjectId, request.DueDate });
        return CreatedAtAction(nameof(Assign), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/override")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<HomeworkAssignmentContract>> OverrideHomework(Guid id, [FromBody] OverrideHomeworkRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return this.ValidationField("overrideReason", "Override reason is required.");

        var entity = await dbContext.HomeworkAssignments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.OverrideForPlatformSupport(request.AudienceId, request.SubjectId, request.Title, request.Instructions, request.DueDate);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("academics.homework.override", entity.SchoolId, new { request.OverrideReason, entity.Id, request.Title });
        return Ok(new HomeworkAssignmentContract(entity.Id, entity.SchoolId, entity.AudienceId, entity.SubjectId, entity.Title, entity.Instructions, entity.DueDate));
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

    public sealed record AssignHomeworkRequest(Guid SchoolId, Guid AudienceId, Guid SubjectId, string Title, string Instructions, DateOnly DueDate);
    public sealed record OverrideHomeworkRequest(Guid AudienceId, Guid SubjectId, string Title, string Instructions, DateOnly DueDate, string OverrideReason);
}

