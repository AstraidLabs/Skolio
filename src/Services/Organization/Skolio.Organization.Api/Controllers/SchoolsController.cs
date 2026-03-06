using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Application.Schools;
using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministration)]
[Route("api/organization/schools")]
public sealed class SchoolsController(IMediator mediator, OrganizationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<SchoolContract>>> List(CancellationToken cancellationToken)
        => Ok(await dbContext.Schools.OrderBy(x => x.Name).Select(x => new SchoolContract(x.Id, x.Name, x.SchoolType)).ToListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SchoolContract>> Detail(Guid id, CancellationToken cancellationToken)
    {
        var school = await dbContext.Schools.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return school is null ? NotFound() : Ok(new SchoolContract(school.Id, school.Name, school.SchoolType));
    }

    [HttpPost]
    [ProducesResponseType(typeof(SchoolContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSchool([FromBody] CreateSchoolRequest request, CancellationToken cancellationToken)
    {
        var contract = await mediator.Send(new CreateSchoolCommand(request.Name, request.SchoolType), cancellationToken);
        return CreatedAtAction(nameof(Detail), new { id = contract.Id }, contract);
    }

    public sealed record CreateSchoolRequest(string Name, SchoolType SchoolType);
}
