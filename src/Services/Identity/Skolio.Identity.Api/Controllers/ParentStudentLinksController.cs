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
    public async Task<ActionResult<IReadOnlyCollection<ParentStudentLinkContract>>> List([FromQuery] Guid? parentUserProfileId, [FromQuery] Guid? studentUserProfileId, CancellationToken cancellationToken)
    {
        var query = dbContext.ParentStudentLinks.AsQueryable();
        if (parentUserProfileId.HasValue)
        {
            query = query.Where(x => x.ParentUserProfileId == parentUserProfileId.Value);
        }

        if (studentUserProfileId.HasValue)
        {
            query = query.Where(x => x.StudentUserProfileId == studentUserProfileId.Value);
        }

        return Ok(await query.Select(x => new ParentStudentLinkContract(x.Id, x.ParentUserProfileId, x.StudentUserProfileId, x.Relationship)).ToListAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ParentStudentLinkContract>> Detail(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ParentStudentLinks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? NotFound() : Ok(new ParentStudentLinkContract(entity.Id, entity.ParentUserProfileId, entity.StudentUserProfileId, entity.Relationship));
    }

    [HttpPost]
    public async Task<ActionResult<ParentStudentLinkContract>> Create([FromBody] CreateParentStudentLinkRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateParentStudentLinkCommand(request.ParentUserProfileId, request.StudentUserProfileId, request.Relationship), cancellationToken);
        return CreatedAtAction(nameof(Detail), new { id = result.Id }, result);
    }

    public sealed record CreateParentStudentLinkRequest(Guid ParentUserProfileId, Guid StudentUserProfileId, string Relationship);
}
