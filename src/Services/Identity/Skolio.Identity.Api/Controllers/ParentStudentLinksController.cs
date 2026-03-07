using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Identity.Application.Contracts;
using Skolio.Identity.Application.ParentStudentLinks;
using Skolio.Identity.Infrastructure.Persistence;

namespace Skolio.Identity.Api.Controllers;

[ApiController]
[Route("api/identity/parent-student-links")]
public sealed class ParentStudentLinksController(IMediator mediator, IdentityDbContext dbContext, ILogger<ParentStudentLinksController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
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
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<ParentStudentLinkContract>> Detail(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ParentStudentLinks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? NotFound() : Ok(new ParentStudentLinkContract(entity.Id, entity.ParentUserProfileId, entity.StudentUserProfileId, entity.Relationship));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.PlatformAdministration)]
    public async Task<ActionResult<ParentStudentLinkContract>> Create([FromBody] CreateParentStudentLinkRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateParentStudentLinkCommand(request.ParentUserProfileId, request.StudentUserProfileId, request.Relationship), cancellationToken);
        Audit("identity.parent-student-link.created", result.Id, new { request.ParentUserProfileId, request.StudentUserProfileId, request.Relationship });
        return CreatedAtAction(nameof(Detail), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/override")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<ParentStudentLinkContract>> Override(Guid id, [FromBody] OverrideParentStudentLinkRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return BadRequest("Override reason is required.");

        var entity = await dbContext.ParentStudentLinks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.UpdateRelationship(request.Relationship);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("identity.parent-student-link.override", id, new { request.Relationship, request.OverrideReason });
        return Ok(new ParentStudentLinkContract(entity.Id, entity.ParentUserProfileId, entity.StudentUserProfileId, entity.Relationship));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Skolio.Identity.Api.Auth.SkolioPolicies.PlatformAdministration)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ParentStudentLinks.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        dbContext.ParentStudentLinks.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        Audit("identity.parent-student-link.deleted", id, new { entity.ParentUserProfileId, entity.StudentUserProfileId });
        return NoContent();
    }

    private void Audit(string actionCode, Guid targetId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} target={TargetId} payload={Payload}", actionCode, actor, targetId, payload);
    }

    public sealed record CreateParentStudentLinkRequest(Guid ParentUserProfileId, Guid StudentUserProfileId, string Relationship);
    public sealed record OverrideParentStudentLinkRequest(string Relationship, string OverrideReason);
}