using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Application.SchoolYears;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministration)]
[Route("api/organization/school-years")]
public sealed class SchoolYearsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(SchoolYearContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSchoolYear([FromBody] CreateSchoolYearRequest request, CancellationToken cancellationToken)
    {
        var contract = await mediator.Send(new CreateSchoolYearCommand(request.SchoolId, request.Label, request.StartDate, request.EndDate), cancellationToken);
        return CreatedAtAction(nameof(CreateSchoolYear), new { id = contract.Id }, contract);
    }

    public sealed record CreateSchoolYearRequest(Guid SchoolId, string Label, DateOnly StartDate, DateOnly EndDate);
}
