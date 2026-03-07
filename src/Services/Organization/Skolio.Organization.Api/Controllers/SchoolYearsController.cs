using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Api.Auth;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Application.SchoolYears;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Route("api/organization/school-years")]
public sealed class SchoolYearsController(IMediator mediator, OrganizationDbContext dbContext, ILogger<SchoolYearsController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<SchoolYearContract>>> List([FromQuery] Guid schoolId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var query = dbContext.SchoolYears.Where(x => x.SchoolId == schoolId);
        if (IsStudentOnly())
        {
            var scopedSchoolYearIds = SchoolScope.GetStudentSchoolYearIds(User);
            if (scopedSchoolYearIds.Count == 0) return Ok(Array.Empty<SchoolYearContract>());
            query = query.Where(x => scopedSchoolYearIds.Contains(x.Id));
        }

        return Ok(await query
            .OrderByDescending(x => x.Period.StartDate)
            .Select(x => new SchoolYearContract(x.Id, x.SchoolId, x.Label, x.Period.StartDate, x.Period.EndDate))
            .ToListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministrationOnly)]
    [ProducesResponseType(typeof(SchoolYearContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSchoolYear([FromBody] CreateSchoolYearRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        var contract = await mediator.Send(new CreateSchoolYearCommand(request.SchoolId, request.Label, request.StartDate, request.EndDate), cancellationToken);
        Audit("organization.school-year.created", request.SchoolId, new { contract.Id, request.Label, request.StartDate, request.EndDate });
        return CreatedAtAction(nameof(CreateSchoolYear), new { id = contract.Id }, contract);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministrationOnly)]
    public async Task<ActionResult<SchoolYearContract>> UpdateSchoolYear(Guid id, [FromBody] UpdateSchoolYearRequest request, CancellationToken cancellationToken)
    {
        var schoolYear = await dbContext.SchoolYears.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (schoolYear is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, schoolYear.SchoolId)) return Forbid();

        schoolYear.UpdatePeriod(request.StartDate, request.EndDate);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("organization.school-year.updated", schoolYear.SchoolId, new { schoolYear.Id, request.StartDate, request.EndDate });
        return Ok(new SchoolYearContract(schoolYear.Id, schoolYear.SchoolId, schoolYear.Label, schoolYear.Period.StartDate, schoolYear.Period.EndDate));
    }

    private void Audit(string actionCode, Guid schoolId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} payload={Payload}", actionCode, actor, schoolId, payload);
    }

    public sealed record CreateSchoolYearRequest(Guid SchoolId, string Label, DateOnly StartDate, DateOnly EndDate);
    public sealed record UpdateSchoolYearRequest(DateOnly StartDate, DateOnly EndDate);

    private bool IsStudentOnly()
        => User.IsInRole("Student") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher") && !User.IsInRole("Parent");
}

