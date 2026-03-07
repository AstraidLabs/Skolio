using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Api.Auth;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Domain.Entities;
using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Route("api/organization/secondary-fields-of-study")]
public sealed class SecondaryFieldsOfStudyController(OrganizationDbContext dbContext, ILogger<SecondaryFieldsOfStudyController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<IReadOnlyCollection<SecondaryFieldOfStudyContract>>> List([FromQuery] Guid schoolId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        return Ok(await dbContext.SecondaryFieldsOfStudy
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.Code)
            .Select(x => new SecondaryFieldOfStudyContract(x.Id, x.SchoolId, x.Code, x.Name))
            .ToListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministrationOnly)]
    public async Task<ActionResult<SecondaryFieldOfStudyContract>> Create([FromBody] CreateSecondaryFieldOfStudyRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        var school = await dbContext.Schools.FirstOrDefaultAsync(x => x.Id == request.SchoolId, cancellationToken);
        if (school is null) return NotFound();
        if (school.SchoolType != SchoolType.SecondarySchool) return BadRequest("Field of study is available only for secondary schools.");

        var field = SecondaryFieldOfStudy.Create(Guid.NewGuid(), request.SchoolId, school.SchoolType, request.Code, request.Name);
        await dbContext.SecondaryFieldsOfStudy.AddAsync(field, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("organization.secondary-field-of-study.created", request.SchoolId, new { field.Id, request.Code, request.Name });
        return CreatedAtAction(nameof(List), new { schoolId = request.SchoolId }, new SecondaryFieldOfStudyContract(field.Id, field.SchoolId, field.Code, field.Name));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministrationOnly)]
    public async Task<ActionResult<SecondaryFieldOfStudyContract>> Update(Guid id, [FromBody] UpdateSecondaryFieldOfStudyRequest request, CancellationToken cancellationToken)
    {
        var field = await dbContext.SecondaryFieldsOfStudy.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (field is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, field.SchoolId)) return Forbid();

        var school = await dbContext.Schools.FirstOrDefaultAsync(x => x.Id == field.SchoolId, cancellationToken);
        if (school is null) return NotFound();

        field.Update(school.SchoolType, request.Code, request.Name);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("organization.secondary-field-of-study.updated", field.SchoolId, new { field.Id, request.Code, request.Name });
        return Ok(new SecondaryFieldOfStudyContract(field.Id, field.SchoolId, field.Code, field.Name));
    }

    private void Audit(string actionCode, Guid schoolId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} payload={Payload}", actionCode, actor, schoolId, payload);
    }

    public sealed record CreateSecondaryFieldOfStudyRequest(Guid SchoolId, string Code, string Name);
    public sealed record UpdateSecondaryFieldOfStudyRequest(string Code, string Name);
}
