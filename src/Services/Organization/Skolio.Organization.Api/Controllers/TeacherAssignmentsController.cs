using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Application.TeacherAssignments;
using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministration)]
[Route("api/organization/teacher-assignments")]
public sealed class TeacherAssignmentsController(IMediator mediator, OrganizationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<TeacherAssignmentContract>>> List([FromQuery] Guid schoolId, CancellationToken cancellationToken)
        => Ok(await dbContext.TeacherAssignments.Where(x => x.SchoolId == schoolId).Select(x => new TeacherAssignmentContract(x.Id, x.SchoolId, x.TeacherUserId, x.Scope, x.ClassRoomId, x.TeachingGroupId, x.SubjectId)).ToListAsync(cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(TeacherAssignmentContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> AssignTeacher([FromBody] AssignTeacherRequest request, CancellationToken cancellationToken)
    {
        var contract = await mediator.Send(new AssignTeacherCommand(request.SchoolId, request.TeacherUserId, request.Scope, request.ClassRoomId, request.TeachingGroupId, request.SubjectId), cancellationToken);
        return CreatedAtAction(nameof(AssignTeacher), new { id = contract.Id }, contract);
    }

    public sealed record AssignTeacherRequest(Guid SchoolId, Guid TeacherUserId, TeacherAssignmentScope Scope, Guid? ClassRoomId, Guid? TeachingGroupId, Guid? SubjectId);
}
