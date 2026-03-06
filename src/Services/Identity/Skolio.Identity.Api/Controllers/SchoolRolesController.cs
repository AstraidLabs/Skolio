using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skolio.Identity.Application.Contracts;
using Skolio.Identity.Application.Roles;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
[Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SchoolAdministration)]
[Route("api/identity/school-role-assignments")]
public sealed class SchoolRolesController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SchoolRoleAssignmentContract>> Assign([FromBody] AssignSchoolRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AssignSchoolRoleCommand(request.UserProfileId, request.SchoolId, request.RoleCode), cancellationToken);
        return CreatedAtAction(nameof(Assign), new { id = result.Id }, result);
    }

    public sealed record AssignSchoolRoleRequest(Guid UserProfileId, Guid SchoolId, string RoleCode);
}
