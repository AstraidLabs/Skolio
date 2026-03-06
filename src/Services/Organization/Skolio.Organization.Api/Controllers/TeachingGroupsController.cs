using MediatR;
using Microsoft.AspNetCore.Mvc;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Application.TeachingGroups;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Route("api/organization/teaching-groups")]
public sealed class TeachingGroupsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(TeachingGroupContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTeachingGroup([FromBody] CreateTeachingGroupRequest request, CancellationToken cancellationToken)
    {
        var contract = await mediator.Send(new CreateTeachingGroupCommand(request.SchoolId, request.ClassRoomId, request.Name, request.IsDailyOperationsGroup), cancellationToken);
        return CreatedAtAction(nameof(CreateTeachingGroup), new { id = contract.Id }, contract);
    }

    public sealed record CreateTeachingGroupRequest(Guid SchoolId, Guid? ClassRoomId, string Name, bool IsDailyOperationsGroup);
}
