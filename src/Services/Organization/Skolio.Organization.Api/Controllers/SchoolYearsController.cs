using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Application.SchoolYears;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministration)]
[Route("api/organization/school-years")]
public sealed class SchoolYearsController(IMediator mediator, OrganizationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<SchoolYearContract>>> List(
        [FromQuery] Guid schoolId,
        CancellationToken cancellationToken)
        => Ok(await dbContext.SchoolYears
            .Where(x => x.SchoolId == schoolId)
            .OrderByDescending(x => x.Period.StartDate)
            .Select(x => new SchoolYearContract(
                x.Id,
                x.SchoolId,
                x.Label,
                x.Period.StartDate,
                x.Period.EndDate))
            .ToListAsync(cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(SchoolYearContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSchoolYear(
        [FromBody] CreateSchoolYearRequest request,
        CancellationToken cancellationToken)
    {
        var contract = await mediator.Send(
            new CreateSchoolYearCommand(request.SchoolId, request.Label, request.StartDate, request.EndDate),
            cancellationToken);

        return CreatedAtAction(nameof(CreateSchoolYear), new { id = contract.Id }, contract);
    }

    public sealed record CreateSchoolYearRequest(Guid SchoolId, string Label, DateOnly StartDate, DateOnly EndDate);
}