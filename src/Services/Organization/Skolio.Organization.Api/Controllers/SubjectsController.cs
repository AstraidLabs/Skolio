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
    private const int MaxPageSize = 200;

    [HttpGet]
    public async Task<ActionResult<PagedResult<SubjectContract>>> List(
        [FromQuery] Guid schoolId,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var normalizedPageNumber = Math.Max(pageNumber, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = dbContext.Subjects.Where(x => x.SchoolId == schoolId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.Name, $"%{term}%") || EF.Functions.ILike(x.Code, $"%{term}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Name)
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(x => new SubjectContract(x.Id, x.SchoolId, x.Code, x.Name))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<SubjectContract>(items, normalizedPageNumber, normalizedPageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SubjectContract>> Detail(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Subjects.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? NotFound() : Ok(new SubjectContract(entity.Id, entity.SchoolId, entity.Code, entity.Name));
    }

    [HttpPost]
    [ProducesResponseType(typeof(SubjectContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectRequest request, CancellationToken cancellationToken)
    {
        var contract = await mediator.Send(new CreateSubjectCommand(request.SchoolId, request.Code, request.Name), cancellationToken);
        return CreatedAtAction(nameof(Detail), new { id = contract.Id }, contract);
    }

    public sealed record CreateSubjectRequest(Guid SchoolId, string Code, string Name);
    public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int PageNumber, int PageSize, int TotalCount);
}
