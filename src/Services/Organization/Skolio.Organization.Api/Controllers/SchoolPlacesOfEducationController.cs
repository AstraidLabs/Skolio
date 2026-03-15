using System.ComponentModel.DataAnnotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Application.SchoolPlaces;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Route("api/organization/school-places-of-education")]
public sealed class SchoolPlacesOfEducationController(
    IMediator mediator,
    OrganizationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<List<SchoolPlaceOfEducationContract>>> List(
        [FromQuery, Required] Guid schoolId,
        CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId))
        {
            return Forbid();
        }

        var places = await dbContext.SchoolPlacesOfEducation
            .Where(x => x.SchoolId == schoolId)
            .OrderByDescending(x => x.IsPrimary).ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return Ok(places.Select(p => new SchoolPlaceOfEducationContract(
            p.Id, p.SchoolId, p.Name,
            new AddressContract(p.Address.Street, p.Address.City, p.Address.PostalCode, p.Address.Country),
            p.Description, p.IsPrimary)).ToList());
    }

    [HttpPost]
    [Authorize(Policy = SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<SchoolPlaceOfEducationContract>> Create(
        [FromBody] CreatePlaceRequest request,
        CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId))
        {
            return Forbid();
        }

        var contract = await mediator.Send(new CreateSchoolPlaceOfEducationCommand(
            request.SchoolId, request.Name,
            new AddressContract(request.Address.Street, request.Address.City, request.Address.PostalCode, request.Address.Country),
            request.Description, request.IsPrimary), cancellationToken);

        return CreatedAtAction(nameof(List), new { schoolId = contract.SchoolId }, contract);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<SchoolPlaceOfEducationContract>> Update(
        Guid id,
        [FromBody] UpdatePlaceRequest request,
        CancellationToken cancellationToken)
    {
        var place = await dbContext.SchoolPlacesOfEducation.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (place is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, place.SchoolId)) return Forbid();

        place.Update(
            request.Name,
            Skolio.Organization.Domain.ValueObjects.Address.Create(request.Address.Street, request.Address.City, request.Address.PostalCode, request.Address.Country),
            request.Description,
            request.IsPrimary);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new SchoolPlaceOfEducationContract(
            place.Id, place.SchoolId, place.Name,
            new AddressContract(place.Address.Street, place.Address.City, place.Address.PostalCode, place.Address.Country),
            place.Description, place.IsPrimary));
    }

    public sealed class AddressInput
    {
        [Required, MaxLength(200)] public string Street { get; init; } = string.Empty;
        [Required, MaxLength(120)] public string City { get; init; } = string.Empty;
        [Required, MaxLength(32)] public string PostalCode { get; init; } = string.Empty;
        [Required, MaxLength(120)] public string Country { get; init; } = string.Empty;
    }

    public sealed class CreatePlaceRequest
    {
        [Required] public Guid SchoolId { get; init; }
        [Required, MaxLength(200)] public string Name { get; init; } = string.Empty;
        [Required] public AddressInput Address { get; init; } = new();
        [MaxLength(500)] public string? Description { get; init; }
        public bool IsPrimary { get; init; }
    }

    public sealed class UpdatePlaceRequest
    {
        [Required, MaxLength(200)] public string Name { get; init; } = string.Empty;
        [Required] public AddressInput Address { get; init; } = new();
        [MaxLength(500)] public string? Description { get; init; }
        public bool IsPrimary { get; init; }
    }
}
