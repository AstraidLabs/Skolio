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
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<ClassRoomContract>>> List([FromQuery] Guid schoolId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var query = dbContext.ClassRooms.Where(x => x.SchoolId == schoolId);
        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();

            var assignedClassIds = await dbContext.TeacherAssignments
                .Where(x => x.SchoolId == schoolId && x.TeacherUserId == actorUserId)
                .Where(x => x.ClassRoomId.HasValue)
                .Select(x => x.ClassRoomId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(x => assignedClassIds.Contains(x.Id));
        }
        else if (IsStudentOnly())
        {
            var scopedClassRoomIds = SchoolScope.GetStudentClassRoomIds(User);
            if (scopedClassRoomIds.Count == 0) return Ok(Array.Empty<ClassRoomContract>());
            query = query.Where(x => scopedClassRoomIds.Contains(x.Id));
        }

        return Ok(await query.OrderBy(x => x.DisplayName).Select(x => new ClassRoomContract(x.Id, x.SchoolId, x.GradeLevelId, x.Code, x.DisplayName)).ToListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministrationOnly)]
    [ProducesResponseType(typeof(ClassRoomContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateClassRoom([FromBody] CreateClassRoomRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        var contract = await mediator.Send(new CreateClassRoomCommand(request.SchoolId, request.GradeLevelId, request.Code, request.DisplayName), cancellationToken);
        Audit("organization.class-room.created", request.SchoolId, new { contract.Id, request.Code, request.DisplayName });
        return CreatedAtAction(nameof(CreateClassRoom), new { id = contract.Id }, contract);
    }

    [HttpPut("{id:guid}/override")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<ClassRoomContract>> OverrideClassRoom(Guid id, [FromBody] OverrideClassRoomRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return this.ValidationField("overrideReason", "Override reason is required.");

        var classRoom = await dbContext.ClassRooms.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (classRoom is null) return NotFound();

        classRoom.OverrideForPlatformSupport(request.Code, request.DisplayName);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("organization.class-room.override", classRoom.SchoolId, new { request.OverrideReason, classRoom.Id, request.Code, request.DisplayName });
        return Ok(new ClassRoomContract(classRoom.Id, classRoom.SchoolId, classRoom.GradeLevelId, classRoom.Code, classRoom.DisplayName));
    }

    private static Guid ResolveActorUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        return Guid.TryParse(raw, out var actorUserId) ? actorUserId : Guid.Empty;
    }

    private Guid ResolveActorUserId() => ResolveActorUserId(User);

    private bool IsStudentOnly()
        => User.IsInRole("Student") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher") && !User.IsInRole("Parent");

    private void Audit(string actionCode, Guid schoolId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} payload={Payload}", actionCode, actor, schoolId, payload);
    }

    public sealed record CreateClassRoomRequest(Guid SchoolId, Guid GradeLevelId, string Code, string DisplayName);
    public sealed record OverrideClassRoomRequest(string Code, string DisplayName, string OverrideReason);
}


