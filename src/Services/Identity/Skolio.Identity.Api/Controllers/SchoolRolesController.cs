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
    public async Task<ActionResult<IReadOnlyCollection<SchoolRoleAssignmentContract>>> List(CancellationToken cancellationToken)
        => Ok(await dbContext.SchoolRoleAssignments.Select(x => new SchoolRoleAssignmentContract(x.Id, x.UserProfileId, x.SchoolId, x.RoleCode)).ToListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<SchoolRoleAssignmentContract>> Assign([FromBody] AssignSchoolRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AssignSchoolRoleCommand(request.UserProfileId, request.SchoolId, request.RoleCode), cancellationToken);
        return CreatedAtAction(nameof(Assign), new { id = result.Id }, result);
    }

    public sealed record AssignSchoolRoleRequest(Guid UserProfileId, Guid SchoolId, string RoleCode);
}
