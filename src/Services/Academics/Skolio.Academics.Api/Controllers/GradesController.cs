using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.ServiceDefaults.Authorization;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.Grades;
using Skolio.Academics.Infrastructure.Persistence;

namespace Skolio.Academics.Api.Controllers;

[ApiController]
[Route("api/academics/grades")]
public sealed class GradesController(IMediator mediator, AcademicsDbContext dbContext, ILogger<GradesController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<GradeEntryContract>>> List([FromQuery] Guid schoolId, [FromQuery] Guid studentUserId, [FromQuery] Guid subjectId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        if (IsParentOnly())
        {
            if (!SchoolScope.GetLinkedStudentIds(User).Contains(studentUserId)) return Forbid();
        }
        else if (IsStudentOnly())
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty || studentUserId != actorUserId) return Forbid();
        }
        else if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();

            var hasTeachingContext = await dbContext.TimetableEntries.AnyAsync(x => x.SchoolId == schoolId && x.TeacherUserId == actorUserId && x.SubjectId == subjectId, cancellationToken);
            if (!hasTeachingContext) return Forbid();
        }

        return Ok(await dbContext.GradeEntries
            .Where(x => x.StudentUserId == studentUserId && x.SubjectId == subjectId)
            .OrderByDescending(x => x.GradedOn)
            .Select(x => new GradeEntryContract(x.Id, x.StudentUserId, x.SubjectId, x.GradeValue, x.Note, x.GradedOn))
            .ToListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<GradeEntryContract>> RecordGrade([FromBody] RecordGradeRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();

            var hasTeachingContext = await dbContext.TimetableEntries.AnyAsync(x => x.SchoolId == request.SchoolId && x.TeacherUserId == actorUserId && x.SubjectId == request.SubjectId, cancellationToken);
            if (!hasTeachingContext) return Forbid();
        }

        var result = await mediator.Send(new RecordGradeEntryCommand(request.StudentUserId, request.SubjectId, request.GradeValue, request.Note, request.GradedOn), cancellationToken);
        Audit("academics.grade.changed", request.SchoolId, new { operation = "create", result.Id, request.StudentUserId, request.SubjectId, request.GradeValue });
        return CreatedAtAction(nameof(RecordGrade), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/override")]
    [Authorize(Policy = SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<GradeEntryContract>> OverrideGrade(Guid id, [FromBody] OverrideGradeRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return this.ValidationField("overrideReason", "Override reason is required.");

        var entity = await dbContext.GradeEntries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.OverrideForPlatformSupport(request.StudentUserId, request.SubjectId, request.GradeValue, request.Note, request.GradedOn);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("academics.grade.override", request.SchoolId, new { request.OverrideReason, entity.Id, request.GradeValue });
        return Ok(new GradeEntryContract(entity.Id, entity.StudentUserId, entity.SubjectId, entity.GradeValue, entity.Note, entity.GradedOn));
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

    public sealed record RecordGradeRequest(Guid SchoolId, Guid StudentUserId, Guid SubjectId, string GradeValue, string Note, DateOnly GradedOn);
    public sealed record OverrideGradeRequest(Guid SchoolId, Guid StudentUserId, Guid SubjectId, string GradeValue, string Note, DateOnly GradedOn, string OverrideReason);
}

