using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Api.Auth;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Application.Schools;
using Skolio.Organization.Application.SchoolYears;
using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Route("api/organization/schools")]
public sealed class SchoolsController(IMediator mediator, OrganizationDbContext dbContext, ILogger<SchoolsController> logger) : ControllerBase
{
    private const int MaxPageSize = 200;

    [HttpGet]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<PagedResult<SchoolContract>>> List(
        [FromQuery] SchoolType? schoolType,
        [FromQuery] bool? isActive,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var normalizedPageNumber = Math.Max(pageNumber, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = dbContext.Schools.AsQueryable();
        if (!SchoolScope.IsPlatformAdministrator(User))
        {
            var scopedSchoolIds = SchoolScope.GetScopedSchoolIds(User);
            query = query.Where(x => scopedSchoolIds.Contains(x.Id));
        }

        if (schoolType.HasValue)
        {
            query = query.Where(x => x.SchoolType == schoolType.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{term}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Name)
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(x => new SchoolContract(x.Id, x.Name, x.SchoolType, x.IsActive, x.SchoolAdministratorUserProfileId))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<SchoolContract>(items, normalizedPageNumber, normalizedPageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<SchoolContract>> Detail(Guid id, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, id)) return Forbid();

        var school = await dbContext.Schools.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return school is null ? NotFound() : Ok(new SchoolContract(school.Id, school.Name, school.SchoolType, school.IsActive, school.SchoolAdministratorUserProfileId));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.PlatformAdministration)]
    [ProducesResponseType(typeof(SchoolContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSchool([FromBody] CreateSchoolRequest request, CancellationToken cancellationToken)
    {
        var contract = await mediator.Send(new CreateSchoolCommand(request.Name, request.SchoolType), cancellationToken);
        Audit("organization.school.created", contract.Id, new { contract.SchoolType });
        return CreatedAtAction(nameof(Detail), new { id = contract.Id }, contract);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<SchoolContract>> UpdateSchool(Guid id, [FromBody] UpdateSchoolRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, id)) return Forbid();

        var school = await dbContext.Schools.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (school is null) return NotFound();

        school.Rename(request.Name);
        school.ChangeSchoolType(request.SchoolType);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("organization.school.updated", school.Id, new { school.SchoolType });
        return Ok(new SchoolContract(school.Id, school.Name, school.SchoolType, school.IsActive, school.SchoolAdministratorUserProfileId));
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.PlatformAdministration)]
    public async Task<ActionResult<SchoolContract>> SetStatus(Guid id, [FromBody] SetSchoolStatusRequest request, CancellationToken cancellationToken)
    {
        var school = await dbContext.Schools.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (school is null) return NotFound();

        if (request.IsActive) school.Activate(); else school.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit(request.IsActive ? "organization.school.activated" : "organization.school.deactivated", school.Id, new { request.IsActive });
        return Ok(new SchoolContract(school.Id, school.Name, school.SchoolType, school.IsActive, school.SchoolAdministratorUserProfileId));
    }

    [HttpPut("{id:guid}/school-administrator")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.PlatformAdministration)]
    public async Task<ActionResult<SchoolContract>> AssignSchoolAdministrator(Guid id, [FromBody] AssignSchoolAdministratorRequest request, CancellationToken cancellationToken)
    {
        var school = await dbContext.Schools.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (school is null) return NotFound();

        school.AssignSchoolAdministrator(request.UserProfileId);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("organization.school.school-administrator.assigned", school.Id, new { request.UserProfileId });
        return Ok(new SchoolContract(school.Id, school.Name, school.SchoolType, school.IsActive, school.SchoolAdministratorUserProfileId));
    }

    [HttpPost("{id:guid}/initial-school-year")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<SchoolYearContract>> CreateInitialSchoolYear(Guid id, [FromBody] CreateInitialSchoolYearRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, id)) return Forbid();

        var schoolExists = await dbContext.Schools.AnyAsync(x => x.Id == id, cancellationToken);
        if (!schoolExists) return NotFound();

        var hasAnySchoolYear = await dbContext.SchoolYears.AnyAsync(x => x.SchoolId == id, cancellationToken);
        if (hasAnySchoolYear) return Conflict("Initial school year already exists.");

        var contract = await mediator.Send(new CreateSchoolYearCommand(id, request.Label, request.StartDate, request.EndDate), cancellationToken);
        Audit("organization.school.initial-school-year.created", id, new { request.Label, request.StartDate, request.EndDate });
        return CreatedAtAction(nameof(CreateInitialSchoolYear), new { id = contract.Id }, contract);
    }

    private void Audit(string actionCode, Guid schoolId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} payload={Payload}", actionCode, actor, schoolId, payload);
    }

    public sealed record CreateSchoolRequest(string Name, SchoolType SchoolType);
    public sealed record UpdateSchoolRequest(string Name, SchoolType SchoolType);
    public sealed record SetSchoolStatusRequest(bool IsActive);
    public sealed record AssignSchoolAdministratorRequest(Guid UserProfileId);
    public sealed record CreateInitialSchoolYearRequest(string Label, DateOnly StartDate, DateOnly EndDate);
    public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int PageNumber, int PageSize, int TotalCount);
}
