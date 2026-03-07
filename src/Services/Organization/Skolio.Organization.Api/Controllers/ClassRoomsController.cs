using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Application.ClassRooms;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Route("api/organization/class-rooms")]
public sealed class ClassRoomsController(IMediator mediator, OrganizationDbContext dbContext, ILogger<ClassRoomsController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<IReadOnlyCollection<ClassRoomContract>>> List([FromQuery] Guid schoolId, CancellationToken cancellationToken)
        => Ok(await dbContext.ClassRooms.Where(x => x.SchoolId == schoolId).OrderBy(x => x.DisplayName).Select(x => new ClassRoomContract(x.Id, x.SchoolId, x.GradeLevelId, x.Code, x.DisplayName)).ToListAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministrationOnly)]
    [ProducesResponseType(typeof(ClassRoomContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateClassRoom([FromBody] CreateClassRoomRequest request, CancellationToken cancellationToken)
    {
        var contract = await mediator.Send(new CreateClassRoomCommand(request.SchoolId, request.GradeLevelId, request.Code, request.DisplayName), cancellationToken);
        return CreatedAtAction(nameof(CreateClassRoom), new { id = contract.Id }, contract);
    }

    [HttpPut("{id:guid}/override")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<ClassRoomContract>> OverrideClassRoom(Guid id, [FromBody] OverrideClassRoomRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return BadRequest("Override reason is required.");

        var classRoom = await dbContext.ClassRooms.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (classRoom is null) return NotFound();

        classRoom.OverrideForPlatformSupport(request.Code, request.DisplayName);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("organization.class-room.override", classRoom.SchoolId, request.OverrideReason, new { classRoom.Id, request.Code, request.DisplayName });
        return Ok(new ClassRoomContract(classRoom.Id, classRoom.SchoolId, classRoom.GradeLevelId, classRoom.Code, classRoom.DisplayName));
    }

    private void Audit(string actionCode, Guid schoolId, string overrideReason, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} overrideReason={OverrideReason} payload={Payload}", actionCode, actor, schoolId, overrideReason, payload);
    }

    public sealed record CreateClassRoomRequest(Guid SchoolId, Guid GradeLevelId, string Code, string DisplayName);
    public sealed record OverrideClassRoomRequest(string Code, string DisplayName, string OverrideReason);
}