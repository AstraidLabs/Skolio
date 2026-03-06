using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Identity.Application.Contracts;
using Skolio.Identity.Application.Roles;
using Skolio.Identity.Infrastructure.Persistence;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
[Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SchoolAdministration)]
[Route("api/identity/school-roles")]
public sealed class SchoolRolesController(IMediator mediator, IdentityDbContext dbContext) : ControllerBase
{
    [HttpGet]
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
    public async Task<ActionResult<SchoolRoleAssignmentContract>> Detail(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.SchoolRoleAssignments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? NotFound() : Ok(new SchoolRoleAssignmentContract(entity.Id, entity.UserProfileId, entity.SchoolId, entity.RoleCode));
    }

    [HttpPost]
    public async Task<ActionResult<SchoolRoleAssignmentContract>> Assign([FromBody] AssignSchoolRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AssignSchoolRoleCommand(request.UserProfileId, request.SchoolId, request.RoleCode), cancellationToken);
        return CreatedAtAction(nameof(Detail), new { id = result.Id }, result);
    }

    public sealed record AssignSchoolRoleRequest(Guid UserProfileId, Guid SchoolId, string RoleCode);
}
