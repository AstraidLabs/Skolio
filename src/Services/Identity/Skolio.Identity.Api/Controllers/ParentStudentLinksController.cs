using MediatR;
using Microsoft.AspNetCore.Mvc;
using Skolio.Identity.Application.Contracts;
using Skolio.Identity.Application.ParentStudentLinks;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
[Route("api/identity/parent-student-links")]
public sealed class ParentStudentLinksController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ParentStudentLinkContract>> Create([FromBody] CreateParentStudentLinkRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateParentStudentLinkCommand(request.ParentUserProfileId, request.StudentUserProfileId, request.Relationship), cancellationToken);
        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
    }

    public sealed record CreateParentStudentLinkRequest(Guid ParentUserProfileId, Guid StudentUserProfileId, string Relationship);
}
