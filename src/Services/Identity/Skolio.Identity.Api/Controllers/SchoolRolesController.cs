using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Identity.Application.Contracts;
using Skolio.Identity.Application.Roles;
using Skolio.Identity.Infrastructure.Persistence;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
[Route("api/identity/school-roles")]
public sealed class SchoolRolesController(IMediator mediator, IdentityDbContext dbContext, ILogger<SchoolRolesController> logger) : ControllerBase
{
    private static readonly HashSet<string> SupportedRoleCodes = ["PlatformAdministrator", "SchoolAdministrator", "Teacher", "Parent", "Student"];

    [HttpGet]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<IReadOnlyCollection<SchoolRoleAssignmentContract>>> List([FromQuery] Guid? schoolId, [FromQuery] string? roleCode, CancellationToken cancellationToken)
    {
        var query = dbContext.SchoolRoleAssignments.AsQueryable();
        if (schoolId.HasValue)
        {
            query = query.Where(x => x.SchoolId == schoolId.Value);
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
        return entity is null ? NotFound() : Ok(new SchoolRoleAssignmentContract(entity.Id, entity.UserProfileId, entity.SchoolId, entity.RoleCode));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.PlatformAdministration)]
    public async Task<ActionResult<SchoolRoleAssignmentContract>> Assign([FromBody] AssignSchoolRoleRequest request, CancellationToken cancellationToken)
    {
        if (!SupportedRoleCodes.Contains(request.RoleCode)) return BadRequest("Unsupported role code.");

        var result = await mediator.Send(new AssignSchoolRoleCommand(request.UserProfileId, request.SchoolId, request.RoleCode), cancellationToken);
        Audit("identity.role-assignment.created", result.Id, new { request.UserProfileId, request.SchoolId, request.RoleCode });
        return CreatedAtAction(nameof(Detail), new { id = result.Id }, result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.PlatformAdministration)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.SchoolRoleAssignments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        dbContext.SchoolRoleAssignments.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("identity.role-assignment.deleted", id, new { entity.UserProfileId, entity.SchoolId, entity.RoleCode });
        return NoContent();
    }

    private void Audit(string actionCode, Guid targetId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} target={TargetId} payload={Payload}", actionCode, actor, targetId, payload);
    }

    public sealed record AssignSchoolRoleRequest(Guid UserProfileId, Guid SchoolId, string RoleCode);
}