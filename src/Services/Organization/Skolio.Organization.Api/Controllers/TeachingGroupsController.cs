using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Application.TeachingGroups;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministration)]
[Route("api/organization/teaching-groups")]
public sealed class TeachingGroupsController(IMediator mediator, OrganizationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<TeachingGroupContract>>> List([FromQuery] Guid schoolId, CancellationToken cancellationToken)
        => Ok(await dbContext.TeachingGroups.Where(x => x.SchoolId == schoolId).OrderBy(x => x.Name).Select(x => new TeachingGroupContract(x.Id, x.SchoolId, x.ClassRoomId, x.Name, x.IsDailyOperationsGroup)).ToListAsync(cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(TeachingGroupContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTeachingGroup([FromBody] CreateTeachingGroupRequest request, CancellationToken cancellationToken)
    {
        var contract = await mediator.Send(new CreateTeachingGroupCommand(request.SchoolId, request.ClassRoomId, request.Name, request.IsDailyOperationsGroup), cancellationToken);
        return CreatedAtAction(nameof(CreateTeachingGroup), new { id = contract.Id }, contract);
    }

    public sealed record CreateTeachingGroupRequest(Guid SchoolId, Guid? ClassRoomId, string Name, bool IsDailyOperationsGroup);
}
