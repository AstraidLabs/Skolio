using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Identity.Application.Contracts;
using Skolio.Identity.Application.ParentStudentLinks;
using Skolio.Identity.Infrastructure.Persistence;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
[Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministration)]
[Route("api/identity/parent-student-links")]
public sealed class ParentStudentLinksController(IMediator mediator, IdentityDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ParentStudentLinkContract>>> List(CancellationToken cancellationToken)
        => Ok(await dbContext.ParentStudentLinks.Select(x => new ParentStudentLinkContract(x.Id, x.ParentUserProfileId, x.StudentUserProfileId, x.Relationship)).ToListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<ParentStudentLinkContract>> Create([FromBody] CreateParentStudentLinkRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateParentStudentLinkCommand(request.ParentUserProfileId, request.StudentUserProfileId, request.Relationship), cancellationToken);
        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
    }

    public sealed record CreateParentStudentLinkRequest(Guid ParentUserProfileId, Guid StudentUserProfileId, string Relationship);
}
