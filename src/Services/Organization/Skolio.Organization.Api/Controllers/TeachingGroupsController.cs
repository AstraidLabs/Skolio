using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Application.TeachingGroups;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Route("api/organization/teaching-groups")]
public sealed class TeachingGroupsController(IMediator mediator, OrganizationDbContext dbContext, ILogger<TeachingGroupsController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<TeachingGroupContract>>> List([FromQuery] Guid schoolId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var query = dbContext.TeachingGroups.Where(x => x.SchoolId == schoolId);
        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();

            var assignments = await dbContext.TeacherAssignments
                .Where(x => x.SchoolId == schoolId && x.TeacherUserId == actorUserId)
                .ToListAsync(cancellationToken);

            var groupIds = assignments.Where(x => x.TeachingGroupId.HasValue).Select(x => x.TeachingGroupId!.Value).ToHashSet();
            var classIds = assignments.Where(x => x.ClassRoomId.HasValue).Select(x => x.ClassRoomId!.Value).ToHashSet();

            query = query.Where(x => groupIds.Contains(x.Id) || (x.ClassRoomId.HasValue && classIds.Contains(x.ClassRoomId.Value)));
        }
        else if (IsStudentOnly())
        {
            var scopedGroupIds = SchoolScope.GetStudentTeachingGroupIds(User);
            if (scopedGroupIds.Count == 0) return Ok(Array.Empty<TeachingGroupContract>());
            query = query.Where(x => scopedGroupIds.Contains(x.Id));
        }

        return Ok(await query.OrderBy(x => x.Name).Select(x => new TeachingGroupContract(x.Id, x.SchoolId, x.ClassRoomId, x.Name, x.IsDailyOperationsGroup)).ToListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministrationOnly)]
    [ProducesResponseType(typeof(TeachingGroupContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTeachingGroup([FromBody] CreateTeachingGroupRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        var contract = await mediator.Send(new CreateTeachingGroupCommand(request.SchoolId, request.ClassRoomId, request.Name, request.IsDailyOperationsGroup), cancellationToken);
        Audit("organization.teaching-group.created", request.SchoolId, new { contract.Id, request.Name, request.IsDailyOperationsGroup });
        return CreatedAtAction(nameof(CreateTeachingGroup), new { id = contract.Id }, contract);
    }

    [HttpPut("{id:guid}/override")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<TeachingGroupContract>> Override(Guid id, [FromBody] OverrideTeachingGroupRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return this.ValidationField("overrideReason", "Override reason is required.");

        var entity = await dbContext.TeachingGroups.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.OverrideForPlatformSupport(request.ClassRoomId, request.Name, request.IsDailyOperationsGroup);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("organization.teaching-group.override", entity.SchoolId, new { request.OverrideReason, entity.Id, request.ClassRoomId, request.Name, request.IsDailyOperationsGroup });
        return Ok(new TeachingGroupContract(entity.Id, entity.SchoolId, entity.ClassRoomId, entity.Name, entity.IsDailyOperationsGroup));
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

    public sealed record CreateTeachingGroupRequest(Guid SchoolId, Guid? ClassRoomId, string Name, bool IsDailyOperationsGroup);
    public sealed record OverrideTeachingGroupRequest(Guid? ClassRoomId, string Name, bool IsDailyOperationsGroup, string OverrideReason);

    private bool IsStudentOnly()
        => User.IsInRole("Student") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher") && !User.IsInRole("Parent");
}


