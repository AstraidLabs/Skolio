using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Application.Schools;
using Skolio.Organization.Domain.Enums;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministration)]
[Route("api/organization/schools")]
public sealed class SchoolsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(SchoolContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSchool([FromBody] CreateSchoolRequest request, CancellationToken cancellationToken)
    {
        var contract = await mediator.Send(new CreateSchoolCommand(request.Name, request.SchoolType), cancellationToken);
        return CreatedAtAction(nameof(CreateSchool), new { id = contract.Id }, contract);
    }

    public sealed record CreateSchoolRequest(string Name, SchoolType SchoolType);
}
