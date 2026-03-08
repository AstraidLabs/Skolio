using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Api.Auth;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Application.TeacherAssignments;
using Skolio.Organization.Domain.Entities;
using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Route("api/organization/teacher-assignments")]
public sealed class TeacherAssignmentsController(IMediator mediator, OrganizationDbContext dbContext, ILogger<TeacherAssignmentsController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<IReadOnlyCollection<TeacherAssignmentContract>>> List([FromQuery] Guid schoolId, [FromQuery] Guid? teacherUserId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var query = dbContext.TeacherAssignments.Where(x => x.SchoolId == schoolId);

        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();

            query = query.Where(x => x.TeacherUserId == actorUserId);
        }
        else if (teacherUserId.HasValue)
        {
            query = query.Where(x => x.TeacherUserId == teacherUserId.Value);
        }

        return Ok(await query
            .Select(x => new TeacherAssignmentContract(x.Id, x.SchoolId, x.TeacherUserId, x.Scope, x.ClassRoomId, x.TeachingGroupId, x.SubjectId))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("me")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<IReadOnlyCollection<TeacherAssignmentContract>>> MyAssignments([FromQuery] Guid schoolId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var actorUserId = ResolveActorUserId();
        if (actorUserId == Guid.Empty) return Forbid();

        var result = await dbContext.TeacherAssignments
            .Where(x => x.SchoolId == schoolId && x.TeacherUserId == actorUserId)
            .Select(x => new TeacherAssignmentContract(x.Id, x.SchoolId, x.TeacherUserId, x.Scope, x.ClassRoomId, x.TeachingGroupId, x.SubjectId))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministrationOnly)]
    [ProducesResponseType(typeof(TeacherAssignmentContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> AssignTeacher([FromBody] AssignTeacherRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        var contract = await mediator.Send(new AssignTeacherCommand(request.SchoolId, request.TeacherUserId, request.Scope, request.ClassRoomId, request.TeachingGroupId, request.SubjectId), cancellationToken);
        Audit("organization.teacher-assignment.changed", request.SchoolId, new { contract.Id, request.Scope, request.TeacherUserId, operation = "create" });
        return CreatedAtAction(nameof(AssignTeacher), new { id = contract.Id }, contract);
    }

    [HttpPost("override/reassign")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<TeacherAssignmentContract>> OverrideReassign([FromBody] OverrideReassignTeacherRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return this.ValidationField("overrideReason", "Override reason is required.");

        if (request.ExistingAssignmentId.HasValue)
        {
            var existing = await dbContext.TeacherAssignments.FirstOrDefaultAsync(x => x.Id == request.ExistingAssignmentId.Value, cancellationToken);
            if (existing is not null)
            {
                dbContext.TeacherAssignments.Remove(existing);
            }
        }

        var assignment = TeacherAssignment.Create(Guid.NewGuid(), request.SchoolId, request.TeacherUserId, request.Scope, request.ClassRoomId, request.TeachingGroupId, request.SubjectId);
        await dbContext.TeacherAssignments.AddAsync(assignment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("organization.teacher-assignment.override", request.SchoolId, new { request.OverrideReason, assignment.Id, request.ExistingAssignmentId });
        return Ok(new TeacherAssignmentContract(assignment.Id, assignment.SchoolId, assignment.TeacherUserId, assignment.Scope, assignment.ClassRoomId, assignment.TeachingGroupId, assignment.SubjectId));
    }

    private Guid ResolveActorUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var actorUserId) ? actorUserId : Guid.Empty;
    }

    private void Audit(string actionCode, Guid schoolId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} payload={Payload}", actionCode, actor, schoolId, payload);
    }

    public sealed record AssignTeacherRequest(Guid SchoolId, Guid TeacherUserId, TeacherAssignmentScope Scope, Guid? ClassRoomId, Guid? TeachingGroupId, Guid? SubjectId);
    public sealed record OverrideReassignTeacherRequest(Guid? ExistingAssignmentId, Guid SchoolId, Guid TeacherUserId, TeacherAssignmentScope Scope, Guid? ClassRoomId, Guid? TeachingGroupId, Guid? SubjectId, string OverrideReason);
}

