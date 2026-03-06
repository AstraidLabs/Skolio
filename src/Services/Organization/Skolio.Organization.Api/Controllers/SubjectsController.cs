using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Application.Subjects;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministration)]
[Route("api/organization/subjects")]
public sealed class SubjectsController(IMediator mediator, OrganizationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<SubjectContract>>> List([FromQuery] Guid schoolId, CancellationToken cancellationToken)
        => Ok(await dbContext.Subjects.Where(x => x.SchoolId == schoolId).OrderBy(x => x.Name).Select(x => new SubjectContract(x.Id, x.SchoolId, x.Code, x.Name)).ToListAsync(cancellationToken));

    [HttpPost]
    [ProducesResponseType(typeof(SubjectContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectRequest request, CancellationToken cancellationToken)
    {
        var contract = await mediator.Send(new CreateSubjectCommand(request.SchoolId, request.Code, request.Name), cancellationToken);
        return CreatedAtAction(nameof(CreateSubject), new { id = contract.Id }, contract);
    }

    public sealed record CreateSubjectRequest(Guid SchoolId, string Code, string Name);
}
