using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Identity.Api.Auth;
using Skolio.Identity.Application.Contracts;
using Skolio.Identity.Application.Roles;
using Skolio.Identity.Infrastructure.Persistence;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
[Route("api/identity/school-roles")]
public sealed class SchoolRolesController(IMediator mediator, IdentityDbContext dbContext, ILogger<SchoolRolesController> logger) : ControllerBase
{
    private static readonly HashSet<string> SupportedRoleCodes = ["PlatformAdministrator", "SchoolAdministrator", "Teacher", "Parent", "Student"];
    private static readonly HashSet<string> SchoolAdministratorManageableRoleCodes = ["Teacher", "Parent", "Student"];

    [HttpGet("student-me")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.StudentSelfService)]
    public async Task<ActionResult<IReadOnlyCollection<SchoolRoleAssignmentContract>>> StudentAssignments(CancellationToken cancellationToken)
    {
        var actorUserId = ResolveActorUserId();
        if (actorUserId == Guid.Empty) return Forbid();

        return Ok(await dbContext.SchoolRoleAssignments
            .Where(x => x.UserProfileId == actorUserId)
            .OrderBy(x => x.RoleCode)
            .Select(x => new SchoolRoleAssignmentContract(x.Id, x.UserProfileId, x.SchoolId, x.RoleCode))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyCollection<SchoolRoleAssignmentContract>>> MyAssignments([FromQuery] Guid? schoolId, CancellationToken cancellationToken)
    {
        var actorUserId = ResolveActorUserId();
        if (actorUserId == Guid.Empty) return Forbid();

        var query = dbContext.SchoolRoleAssignments.Where(x => x.UserProfileId == actorUserId);
        if (schoolId.HasValue)
        {
            if (!SchoolScope.HasSchoolAccess(User, schoolId.Value)) return Forbid();
            query = query.Where(x => x.SchoolId == schoolId.Value);
        }
        else if (!SchoolScope.IsPlatformAdministrator(User))
        {
            var scopedSchoolIds = SchoolScope.GetScopedSchoolIds(User);
            query = query.Where(x => scopedSchoolIds.Contains(x.SchoolId));
        }

        return Ok(await query
            .OrderBy(x => x.RoleCode)
            .Select(x => new SchoolRoleAssignmentContract(x.Id, x.UserProfileId, x.SchoolId, x.RoleCode))
            .ToListAsync(cancellationToken));
    }

    [HttpGet]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<IReadOnlyCollection<SchoolRoleAssignmentContract>>> List([FromQuery] Guid? schoolId, [FromQuery] string? roleCode, CancellationToken cancellationToken)
    {
        var query = dbContext.SchoolRoleAssignments.AsQueryable();
        if (schoolId.HasValue)
        {
            if (!SchoolScope.HasSchoolAccess(User, schoolId.Value)) return Forbid();
            query = query.Where(x => x.SchoolId == schoolId.Value);
        }
        else if (!SchoolScope.IsPlatformAdministrator(User))
        {
            var scopedSchoolIds = SchoolScope.GetScopedSchoolIds(User);
            query = query.Where(x => scopedSchoolIds.Contains(x.SchoolId));
        }

        if (!string.IsNullOrWhiteSpace(roleCode))
        {
            query = query.Where(x => x.RoleCode == roleCode);
        }

        return Ok(await query
            .OrderBy(x => x.RoleCode)
            .Select(x => new SchoolRoleAssignmentContract(x.Id, x.UserProfileId, x.SchoolId, x.RoleCode))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<SchoolRoleAssignmentContract>> Detail(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.SchoolRoleAssignments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, entity.SchoolId)) return Forbid();

        return Ok(new SchoolRoleAssignmentContract(entity.Id, entity.UserProfileId, entity.SchoolId, entity.RoleCode));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<SchoolRoleAssignmentContract>> Assign([FromBody] AssignSchoolRoleRequest request, CancellationToken cancellationToken)
    {
        if (!SupportedRoleCodes.Contains(request.RoleCode)) return this.ValidationField("roleCode", "Unsupported role code.");
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        if (!SchoolScope.IsPlatformAdministrator(User) && !SchoolAdministratorManageableRoleCodes.Contains(request.RoleCode))
        {
            return Forbid();
        }

        var result = await mediator.Send(new AssignSchoolRoleCommand(request.UserProfileId, request.SchoolId, request.RoleCode), cancellationToken);
        Audit("identity.role-assignment.changed", result.Id, new { request.UserProfileId, request.SchoolId, request.RoleCode, operation = "assign" });
        return CreatedAtAction(nameof(Detail), new { id = result.Id }, result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.SchoolRoleAssignments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, entity.SchoolId)) return Forbid();

        if (!SchoolScope.IsPlatformAdministrator(User) && !SchoolAdministratorManageableRoleCodes.Contains(entity.RoleCode))
        {
            return Forbid();
        }

        dbContext.SchoolRoleAssignments.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("identity.role-assignment.changed", id, new { entity.UserProfileId, entity.SchoolId, entity.RoleCode, operation = "delete" });
        return NoContent();
    }

    private Guid ResolveActorUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var actorUserId) ? actorUserId : Guid.Empty;
    }

    private void Audit(string actionCode, Guid targetId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} target={TargetId} payload={Payload}", actionCode, actor, targetId, payload);
    }

    public sealed record AssignSchoolRoleRequest(Guid UserProfileId, Guid SchoolId, string RoleCode);
}

