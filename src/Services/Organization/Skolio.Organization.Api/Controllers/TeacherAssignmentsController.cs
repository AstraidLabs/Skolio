using MediatR;
using Microsoft.AspNetCore.Mvc;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Application.TeacherAssignments;
using Skolio.Organization.Domain.Enums;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Route("api/organization/teacher-assignments")]
public sealed class TeacherAssignmentsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(TeacherAssignmentContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> AssignTeacher([FromBody] AssignTeacherRequest request, CancellationToken cancellationToken)
    {
        var contract = await mediator.Send(
            new AssignTeacherCommand(request.SchoolId, request.TeacherUserId, request.Scope, request.ClassRoomId, request.TeachingGroupId, request.SubjectId),
            cancellationToken);

        return CreatedAtAction(nameof(AssignTeacher), new { id = contract.Id }, contract);
    }

    public sealed record AssignTeacherRequest(Guid SchoolId, Guid TeacherUserId, TeacherAssignmentScope Scope, Guid? ClassRoomId, Guid? TeachingGroupId, Guid? SubjectId);
}
