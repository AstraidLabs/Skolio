using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Api.Auth;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Domain.Entities;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Route("api/organization/grade-levels")]
public sealed class GradeLevelsController(OrganizationDbContext dbContext, ILogger<GradeLevelsController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<IReadOnlyCollection<GradeLevelContract>>> List([FromQuery] Guid schoolId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        return Ok(await dbContext.GradeLevels
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.Level)
            .Select(x => new GradeLevelContract(x.Id, x.SchoolId, x.Level, x.DisplayName))
            .ToListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministrationOnly)]
    public async Task<ActionResult<GradeLevelContract>> Create([FromBody] CreateGradeLevelRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        var school = await dbContext.Schools.FirstOrDefaultAsync(x => x.Id == request.SchoolId, cancellationToken);
        if (school is null) return NotFound();

        var gradeLevel = GradeLevel.Create(Guid.NewGuid(), request.SchoolId, school.SchoolType, request.Level, request.DisplayName);
        await dbContext.GradeLevels.AddAsync(gradeLevel, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("organization.grade-level.created", request.SchoolId, new { gradeLevel.Id, request.Level, request.DisplayName });
        return CreatedAtAction(nameof(List), new { schoolId = request.SchoolId }, new GradeLevelContract(gradeLevel.Id, gradeLevel.SchoolId, gradeLevel.Level, gradeLevel.DisplayName));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministrationOnly)]
    public async Task<ActionResult<GradeLevelContract>> Update(Guid id, [FromBody] UpdateGradeLevelRequest request, CancellationToken cancellationToken)
    {
        var gradeLevel = await dbContext.GradeLevels.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (gradeLevel is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, gradeLevel.SchoolId)) return Forbid();

        var school = await dbContext.Schools.FirstOrDefaultAsync(x => x.Id == gradeLevel.SchoolId, cancellationToken);
        if (school is null) return NotFound();

        gradeLevel.Update(school.SchoolType, request.Level, request.DisplayName);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("organization.grade-level.updated", gradeLevel.SchoolId, new { gradeLevel.Id, request.Level, request.DisplayName });
        return Ok(new GradeLevelContract(gradeLevel.Id, gradeLevel.SchoolId, gradeLevel.Level, gradeLevel.DisplayName));
    }

    private void Audit(string actionCode, Guid schoolId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} payload={Payload}", actionCode, actor, schoolId, payload);
    }

    public sealed record CreateGradeLevelRequest(Guid SchoolId, int Level, string DisplayName);
    public sealed record UpdateGradeLevelRequest(int Level, string DisplayName);
}
