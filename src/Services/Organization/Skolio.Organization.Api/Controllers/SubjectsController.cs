using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Application.Subjects;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministration)]
[Route("api/organization/subjects")]
public sealed class SubjectsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(SubjectContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectRequest request, CancellationToken cancellationToken)
    {
        var contract = await mediator.Send(new CreateSubjectCommand(request.SchoolId, request.Code, request.Name), cancellationToken);
        return CreatedAtAction(nameof(CreateSubject), new { id = contract.Id }, contract);
    }

    public sealed record CreateSubjectRequest(Guid SchoolId, string Code, string Name);
}
