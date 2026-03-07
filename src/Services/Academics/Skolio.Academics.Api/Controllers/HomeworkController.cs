using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Academics.Api.Auth;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.Homework;
using Skolio.Academics.Infrastructure.Persistence;

namespace Skolio.Academics.Api.Controllers;

[ApiController]
[Route("api/academics/homework")]
public sealed class HomeworkController(IMediator mediator, AcademicsDbContext dbContext, ILogger<HomeworkController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<IReadOnlyCollection<HomeworkAssignmentContract>>> List([FromQuery] Guid schoolId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        return Ok(await dbContext.HomeworkAssignments.Where(x => x.SchoolId == schoolId).OrderBy(x => x.DueDate).Select(x => new HomeworkAssignmentContract(x.Id, x.SchoolId, x.AudienceId, x.SubjectId, x.Title, x.Instructions, x.DueDate)).ToListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<HomeworkAssignmentContract>> Assign([FromBody] AssignHomeworkRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        var result = await mediator.Send(new AssignHomeworkCommand(request.SchoolId, request.AudienceId, request.SubjectId, request.Title, request.Instructions, request.DueDate), cancellationToken);
        return CreatedAtAction(nameof(Assign), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/override")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<HomeworkAssignmentContract>> OverrideHomework(Guid id, [FromBody] OverrideHomeworkRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return BadRequest("Override reason is required.");

        var entity = await dbContext.HomeworkAssignments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.OverrideForPlatformSupport(request.AudienceId, request.SubjectId, request.Title, request.Instructions, request.DueDate);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("academics.homework.override", entity.SchoolId, new { request.OverrideReason, entity.Id, request.Title });
        return Ok(new HomeworkAssignmentContract(entity.Id, entity.SchoolId, entity.AudienceId, entity.SubjectId, entity.Title, entity.Instructions, entity.DueDate));
    }

    private void Audit(string actionCode, Guid schoolId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} payload={Payload}", actionCode, actor, schoolId, payload);
    }

    public sealed record AssignHomeworkRequest(Guid SchoolId, Guid AudienceId, Guid SubjectId, string Title, string Instructions, DateOnly DueDate);
    public sealed record OverrideHomeworkRequest(Guid AudienceId, Guid SubjectId, string Title, string Instructions, DateOnly DueDate, string OverrideReason);
}
