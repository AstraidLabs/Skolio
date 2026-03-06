using MediatR;
using Microsoft.AspNetCore.Mvc;
using Skolio.Organization.Application.ClassRooms;
using Skolio.Organization.Application.Contracts;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Route("api/organization/class-rooms")]
public sealed class ClassRoomsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ClassRoomContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateClassRoom([FromBody] CreateClassRoomRequest request, CancellationToken cancellationToken)
    {
        var contract = await mediator.Send(new CreateClassRoomCommand(request.SchoolId, request.GradeLevelId, request.Code, request.DisplayName), cancellationToken);
        return CreatedAtAction(nameof(CreateClassRoom), new { id = contract.Id }, contract);
    }

    public sealed record CreateClassRoomRequest(Guid SchoolId, Guid GradeLevelId, string Code, string DisplayName);
}
