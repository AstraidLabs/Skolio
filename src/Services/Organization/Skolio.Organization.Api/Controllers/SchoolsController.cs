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
    private const int MaxPageSize = 200;

    [HttpGet]
    public async Task<ActionResult<PagedResult<SchoolContract>>> List(
        [FromQuery] SchoolType? schoolType,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var normalizedPageNumber = Math.Max(pageNumber, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = dbContext.Schools.AsQueryable();

        if (schoolType.HasValue)
        {
            query = query.Where(x => x.SchoolType == schoolType.Value);
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
            .Select(x => new SchoolContract(x.Id, x.Name, x.SchoolType))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<SchoolContract>(items, normalizedPageNumber, normalizedPageSize, totalCount));
    }

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
    public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int PageNumber, int PageSize, int TotalCount);
}
