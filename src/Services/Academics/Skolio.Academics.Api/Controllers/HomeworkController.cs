using MediatR;
using Microsoft.AspNetCore.Mvc;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.Homework;

namespace Skolio.Academics.Api.Controllers;
[ApiController]
[Route("api/academics/homework")]
public sealed class HomeworkController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<HomeworkAssignmentContract>> Assign([FromBody] AssignHomeworkRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AssignHomeworkCommand(request.SchoolId, request.AudienceId, request.SubjectId, request.Title, request.Instructions, request.DueDate), cancellationToken);
        return CreatedAtAction(nameof(Assign), new { id = result.Id }, result);
    }

    public sealed record AssignHomeworkRequest(Guid SchoolId, Guid AudienceId, Guid SubjectId, string Title, string Instructions, DateOnly DueDate);
}
