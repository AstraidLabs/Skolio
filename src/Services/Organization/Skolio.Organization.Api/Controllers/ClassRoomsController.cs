using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Application.ClassRooms;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministration)]
[Route("api/organization/class-rooms")]
public sealed class ClassRoomsController(IMediator mediator, OrganizationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ClassRoomContract>>> List([FromQuery] Guid schoolId, CancellationToken cancellationToken)
        => Ok(await dbContext.ClassRooms.Where(x => x.SchoolId == schoolId).OrderBy(x => x.DisplayName).Select(x => new ClassRoomContract(x.Id, x.SchoolId, x.GradeLevelId, x.Code, x.DisplayName)).ToListAsync(cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(ClassRoomContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateClassRoom([FromBody] CreateClassRoomRequest request, CancellationToken cancellationToken)
    {
        var contract = await mediator.Send(new CreateClassRoomCommand(request.SchoolId, request.GradeLevelId, request.Code, request.DisplayName), cancellationToken);
        return CreatedAtAction(nameof(CreateClassRoom), new { id = contract.Id }, contract);
    }

    public sealed record CreateClassRoomRequest(Guid SchoolId, Guid GradeLevelId, string Code, string DisplayName);
}
