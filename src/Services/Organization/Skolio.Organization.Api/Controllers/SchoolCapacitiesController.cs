using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.ServiceDefaults.Authorization;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Application.SchoolCapacities;
using Skolio.Organization.Domain.Enums;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Route("api/organization/school-capacities")]
public sealed class SchoolCapacitiesController(
    IMediator mediator,
    OrganizationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<List<SchoolCapacityContract>>> List(
        [FromQuery, Required] Guid schoolId,
        CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId))
        {
            return Forbid();
        }

        var capacities = await dbContext.SchoolCapacities
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.CapacityType)
            .ToListAsync(cancellationToken);

        return Ok(capacities.Select(c => new SchoolCapacityContract(
            c.Id, c.SchoolId, c.CapacityType, c.MaxCapacity, c.Description)).ToList());
    }

    [HttpPost]
    [Authorize(Policy = SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<SchoolCapacityContract>> Create(
        [FromBody] CreateCapacityRequest request,
        CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId))
        {
            return Forbid();
        }

        var contract = await mediator.Send(new CreateSchoolCapacityCommand(
            request.SchoolId, request.CapacityType, request.MaxCapacity, request.Description), cancellationToken);

        return CreatedAtAction(nameof(List), new { schoolId = contract.SchoolId }, contract);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<SchoolCapacityContract>> Update(
        Guid id,
        [FromBody] UpdateCapacityRequest request,
        CancellationToken cancellationToken)
    {
        var capacity = await dbContext.SchoolCapacities.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (capacity is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, capacity.SchoolId)) return Forbid();

        capacity.Update(request.CapacityType, request.MaxCapacity, request.Description);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new SchoolCapacityContract(capacity.Id, capacity.SchoolId, capacity.CapacityType, capacity.MaxCapacity, capacity.Description));
    }

    public sealed class CreateCapacityRequest
    {
        [Required] public Guid SchoolId { get; init; }
        [Required] public SchoolCapacityType CapacityType { get; init; }
        [Required, Range(1, 100000)] public int MaxCapacity { get; init; }
        [MaxLength(500)] public string? Description { get; init; }
    }

    public sealed class UpdateCapacityRequest
    {
        [Required] public SchoolCapacityType CapacityType { get; init; }
        [Required, Range(1, 100000)] public int MaxCapacity { get; init; }
        [MaxLength(500)] public string? Description { get; init; }
    }
}
