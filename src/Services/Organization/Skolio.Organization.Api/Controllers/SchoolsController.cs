using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Api.Auth;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Application.Schools;
using Skolio.Organization.Application.SchoolYears;
using Skolio.Organization.Domain.Entities;
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

        IQueryable<School> query = dbContext.Schools
            .AsQueryable()
            .Include(x => x.SchoolOperator)
            .Include(x => x.Founder);

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
        var schools = await query.OrderBy(x => x.Name)
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = schools.Select(MapSchool).ToList();
        return Ok(new PagedResult<SchoolContract>(items, normalizedPageNumber, normalizedPageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<SchoolContract>> Detail(Guid id, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, id))
        {
            return Forbid();
        }

        var school = await dbContext.Schools
            .Include(x => x.SchoolOperator)
            .Include(x => x.Founder)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return school is null ? NotFound() : Ok(MapSchool(school));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.PlatformAdministration)]
    [ProducesResponseType(typeof(SchoolContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSchool([FromBody] CreateSchoolRequest request, CancellationToken cancellationToken)
    {
        if (!IsValidIco(request.SchoolOperator.CompanyNumberIco) || !IsValidIco(request.Founder.FounderIco))
        {
            return this.ValidationForm("ICO must contain exactly 8 digits when provided.");
        }

        var contract = await mediator.Send(new CreateSchoolCommand(
            request.Name,
            request.SchoolType,
            request.SchoolKind,
            request.SchoolIzo,
            request.SchoolEmail,
            request.SchoolPhone,
            request.SchoolWebsite,
            new AddressContract(request.MainAddress.Street, request.MainAddress.City, request.MainAddress.PostalCode, request.MainAddress.Country),
            request.EducationLocationsSummary,
            request.RegistryEntryDate,
            request.EducationStartDate,
            request.MaxStudentCapacity,
            request.TeachingLanguage,
            request.PlatformStatus,
            new SchoolOperatorContract(
                Guid.Empty,
                request.SchoolOperator.LegalEntityName,
                request.SchoolOperator.LegalForm,
                request.SchoolOperator.CompanyNumberIco,
                new AddressContract(
                    request.SchoolOperator.RegisteredOfficeAddress.Street,
                    request.SchoolOperator.RegisteredOfficeAddress.City,
                    request.SchoolOperator.RegisteredOfficeAddress.PostalCode,
                    request.SchoolOperator.RegisteredOfficeAddress.Country),
                request.SchoolOperator.ResortIdentifier,
                request.SchoolOperator.DirectorSummary,
                request.SchoolOperator.StatutoryBodySummary),
            new FounderContract(
                Guid.Empty,
                request.Founder.FounderType,
                request.Founder.FounderCategory,
                request.Founder.FounderName,
                request.Founder.FounderLegalForm,
                request.Founder.FounderIco,
                new AddressContract(
                    request.Founder.FounderAddress.Street,
                    request.Founder.FounderAddress.City,
                    request.Founder.FounderAddress.PostalCode,
                    request.Founder.FounderAddress.Country),
                request.Founder.FounderEmail)), cancellationToken);

        Audit("organization.school.created", contract.Id, new { contract.SchoolType, contract.PlatformStatus });
        return CreatedAtAction(nameof(Detail), new { id = contract.Id }, contract);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<SchoolContract>> UpdateSchool(Guid id, [FromBody] UpdateSchoolRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, id))
        {
            return Forbid();
        }

        if (!IsValidIco(request.SchoolOperator.CompanyNumberIco) || !IsValidIco(request.Founder.FounderIco))
        {
            return this.ValidationForm("ICO must contain exactly 8 digits when provided.");
        }

        var school = await dbContext.Schools
            .Include(x => x.SchoolOperator)
            .Include(x => x.Founder)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (school is null)
        {
            return NotFound();
        }

        var schoolOperator = school.SchoolOperator;
        if (schoolOperator is null)
        {
            schoolOperator = SchoolOperator.Create(
                Guid.NewGuid(),
                request.SchoolOperator.LegalEntityName,
                request.SchoolOperator.LegalForm,
                request.SchoolOperator.CompanyNumberIco,
                Skolio.Organization.Domain.ValueObjects.Address.Create(
                    request.SchoolOperator.RegisteredOfficeAddress.Street,
                    request.SchoolOperator.RegisteredOfficeAddress.City,
                    request.SchoolOperator.RegisteredOfficeAddress.PostalCode,
                    request.SchoolOperator.RegisteredOfficeAddress.Country),
                request.SchoolOperator.ResortIdentifier,
                request.SchoolOperator.DirectorSummary,
                request.SchoolOperator.StatutoryBodySummary);
            dbContext.SchoolOperators.Add(schoolOperator);
        }
        else
        {
            schoolOperator.Update(
                request.SchoolOperator.LegalEntityName,
                request.SchoolOperator.LegalForm,
                request.SchoolOperator.CompanyNumberIco,
                Skolio.Organization.Domain.ValueObjects.Address.Create(
                    request.SchoolOperator.RegisteredOfficeAddress.Street,
                    request.SchoolOperator.RegisteredOfficeAddress.City,
                    request.SchoolOperator.RegisteredOfficeAddress.PostalCode,
                    request.SchoolOperator.RegisteredOfficeAddress.Country),
                request.SchoolOperator.ResortIdentifier,
                request.SchoolOperator.DirectorSummary,
                request.SchoolOperator.StatutoryBodySummary);
        }

        var founder = school.Founder;
        if (founder is null)
        {
            founder = Founder.Create(
                Guid.NewGuid(),
                request.Founder.FounderType,
                request.Founder.FounderCategory,
                request.Founder.FounderName,
                request.Founder.FounderLegalForm,
                request.Founder.FounderIco,
                Skolio.Organization.Domain.ValueObjects.Address.Create(
                    request.Founder.FounderAddress.Street,
                    request.Founder.FounderAddress.City,
                    request.Founder.FounderAddress.PostalCode,
                    request.Founder.FounderAddress.Country),
                request.Founder.FounderEmail);
            dbContext.Founders.Add(founder);
        }
        else
        {
            founder.Update(
                request.Founder.FounderType,
                request.Founder.FounderCategory,
                request.Founder.FounderName,
                request.Founder.FounderLegalForm,
                request.Founder.FounderIco,
                Skolio.Organization.Domain.ValueObjects.Address.Create(
                    request.Founder.FounderAddress.Street,
                    request.Founder.FounderAddress.City,
                    request.Founder.FounderAddress.PostalCode,
                    request.Founder.FounderAddress.Country),
                request.Founder.FounderEmail);
        }

        school.Rename(request.Name);
        school.ChangeSchoolType(request.SchoolType);
        school.UpdateIdentityAndOperations(
            request.SchoolKind,
            request.SchoolIzo,
            request.SchoolEmail,
            request.SchoolPhone,
            request.SchoolWebsite,
            Skolio.Organization.Domain.ValueObjects.Address.Create(request.MainAddress.Street, request.MainAddress.City, request.MainAddress.PostalCode, request.MainAddress.Country),
            request.EducationLocationsSummary,
            request.RegistryEntryDate,
            request.EducationStartDate,
            request.MaxStudentCapacity,
            request.TeachingLanguage,
            schoolOperator.Id,
            founder.Id,
            request.PlatformStatus);

        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("organization.school.updated", school.Id, new { school.SchoolType, school.PlatformStatus });

        school = await dbContext.Schools.Include(x => x.SchoolOperator).Include(x => x.Founder)
            .FirstAsync(x => x.Id == school.Id, cancellationToken);

        return Ok(MapSchool(school));
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.PlatformAdministration)]
    public async Task<ActionResult<SchoolContract>> SetStatus(Guid id, [FromBody] SetSchoolStatusRequest request, CancellationToken cancellationToken)
    {
        var school = await dbContext.Schools
            .Include(x => x.SchoolOperator)
            .Include(x => x.Founder)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (school is null)
        {
            return NotFound();
        }

        if (request.IsActive)
        {
            school.Activate();
        }
        else
        {
            school.Deactivate();
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        Audit(request.IsActive ? "organization.school.activated" : "organization.school.deactivated", school.Id, new { request.IsActive });
        return Ok(MapSchool(school));
    }

    [HttpPut("{id:guid}/school-administrator")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.PlatformAdministration)]
    public async Task<ActionResult<SchoolContract>> AssignSchoolAdministrator(Guid id, [FromBody] AssignSchoolAdministratorRequest request, CancellationToken cancellationToken)
    {
        var school = await dbContext.Schools
            .Include(x => x.SchoolOperator)
            .Include(x => x.Founder)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (school is null)
        {
            return NotFound();
        }

        school.AssignSchoolAdministrator(request.UserProfileId);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("organization.school.school-administrator.assigned", school.Id, new { request.UserProfileId });
        return Ok(MapSchool(school));
    }

    [HttpPost("{id:guid}/initial-school-year")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<SchoolYearContract>> CreateInitialSchoolYear(Guid id, [FromBody] CreateInitialSchoolYearRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, id))
        {
            return Forbid();
        }

        var schoolExists = await dbContext.Schools.AnyAsync(x => x.Id == id, cancellationToken);
        if (!schoolExists)
        {
            return NotFound();
        }

        var hasAnySchoolYear = await dbContext.SchoolYears.AnyAsync(x => x.SchoolId == id, cancellationToken);
        if (hasAnySchoolYear)
        {
            return Conflict("Initial school year already exists.");
        }

        var contract = await mediator.Send(new CreateSchoolYearCommand(id, request.Label, request.StartDate, request.EndDate), cancellationToken);
        Audit("organization.school.initial-school-year.created", id, new { request.Label, request.StartDate, request.EndDate });
        return CreatedAtAction(nameof(CreateInitialSchoolYear), new { id = contract.Id }, contract);
    }

    private static SchoolContract MapSchool(School school)
    {
        var mainAddress = new AddressContract(
            school.MainAddress.Street,
            school.MainAddress.City,
            school.MainAddress.PostalCode,
            school.MainAddress.Country);

        var schoolOperator = school.SchoolOperator is null
            ? null
            : new SchoolOperatorContract(
                school.SchoolOperator.Id,
                school.SchoolOperator.LegalEntityName,
                school.SchoolOperator.LegalForm,
                school.SchoolOperator.CompanyNumberIco,
                new AddressContract(
                    school.SchoolOperator.RegisteredOfficeAddress.Street,
                    school.SchoolOperator.RegisteredOfficeAddress.City,
                    school.SchoolOperator.RegisteredOfficeAddress.PostalCode,
                    school.SchoolOperator.RegisteredOfficeAddress.Country),
                school.SchoolOperator.ResortIdentifier,
                school.SchoolOperator.DirectorSummary,
                school.SchoolOperator.StatutoryBodySummary);

        var founder = school.Founder is null
            ? null
            : new FounderContract(
                school.Founder.Id,
                school.Founder.FounderType,
                school.Founder.FounderCategory,
                school.Founder.FounderName,
                school.Founder.FounderLegalForm,
                school.Founder.FounderIco,
                new AddressContract(
                    school.Founder.FounderAddress.Street,
                    school.Founder.FounderAddress.City,
                    school.Founder.FounderAddress.PostalCode,
                    school.Founder.FounderAddress.Country),
                school.Founder.FounderEmail);

        return new SchoolContract(
            school.Id,
            school.Name,
            school.SchoolType,
            school.SchoolKind,
            school.SchoolIzo,
            school.SchoolEmail,
            school.SchoolPhone,
            school.SchoolWebsite,
            mainAddress,
            school.EducationLocationsSummary,
            school.RegistryEntryDate,
            school.EducationStartDate,
            school.MaxStudentCapacity,
            school.TeachingLanguage,
            school.SchoolOperatorId,
            school.FounderId,
            school.PlatformStatus,
            school.IsActive,
            school.SchoolAdministratorUserProfileId,
            schoolOperator,
            founder);
    }

    private void Audit(string actionCode, Guid schoolId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} payload={Payload}", actionCode, actor, schoolId, payload);
    }

    private static bool IsValidIco(string? value)
        => string.IsNullOrWhiteSpace(value) || (value.Length == 8 && value.All(char.IsDigit));

    public sealed class AddressRequest
    {
        [Required]
        [MaxLength(200)]
        public string Street { get; init; } = string.Empty;

        [Required]
        [MaxLength(120)]
        public string City { get; init; } = string.Empty;

        [Required]
        [MaxLength(32)]
        public string PostalCode { get; init; } = string.Empty;

        [Required]
        [MaxLength(120)]
        public string Country { get; init; } = string.Empty;
    }

    public sealed class SchoolOperatorRequest
    {
        [Required]
        [MaxLength(200)]
        public string LegalEntityName { get; init; } = string.Empty;

        [Required]
        public LegalForm LegalForm { get; init; }

        [RegularExpression("^\\d{8}$")]
        public string? CompanyNumberIco { get; init; }

        [Required]
        public AddressRequest RegisteredOfficeAddress { get; init; } = new();

        [MaxLength(64)]
        public string? ResortIdentifier { get; init; }

        [MaxLength(300)]
        public string? DirectorSummary { get; init; }

        [MaxLength(600)]
        public string? StatutoryBodySummary { get; init; }
    }

    public sealed class FounderRequest
    {
        [Required]
        public FounderType FounderType { get; init; }

        [Required]
        public FounderCategory FounderCategory { get; init; }

        [Required]
        [MaxLength(200)]
        public string FounderName { get; init; } = string.Empty;

        [Required]
        public LegalForm FounderLegalForm { get; init; }

        [RegularExpression("^\\d{8}$")]
        public string? FounderIco { get; init; }

        [Required]
        public AddressRequest FounderAddress { get; init; } = new();

        [EmailAddress]
        [MaxLength(256)]
        public string? FounderEmail { get; init; }
    }

    public class CreateSchoolRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; init; } = string.Empty;

        [Required]
        public SchoolType SchoolType { get; init; }

        [Required]
        public SchoolKind SchoolKind { get; init; } = SchoolKind.General;

        [MaxLength(32)]
        public string? SchoolIzo { get; init; }

        [EmailAddress]
        [MaxLength(256)]
        public string? SchoolEmail { get; init; }

        [MaxLength(64)]
        public string? SchoolPhone { get; init; }

        [MaxLength(256)]
        public string? SchoolWebsite { get; init; }

        [Required]
        public AddressRequest MainAddress { get; init; } = new();

        [MaxLength(1000)]
        public string? EducationLocationsSummary { get; init; }

        public DateOnly? RegistryEntryDate { get; init; }
        public DateOnly? EducationStartDate { get; init; }

        [Range(1, 100000)]
        public int? MaxStudentCapacity { get; init; }

        [MaxLength(64)]
        public string? TeachingLanguage { get; init; }

        [Required]
        public PlatformStatus PlatformStatus { get; init; } = PlatformStatus.Active;

        [Required]
        public SchoolOperatorRequest SchoolOperator { get; init; } = new();

        [Required]
        public FounderRequest Founder { get; init; } = new();
    }

    public sealed class UpdateSchoolRequest : CreateSchoolRequest;

    public sealed record SetSchoolStatusRequest(bool IsActive);
    public sealed record AssignSchoolAdministratorRequest(Guid UserProfileId);
    public sealed record CreateInitialSchoolYearRequest(string Label, DateOnly StartDate, DateOnly EndDate);
    public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int PageNumber, int PageSize, int TotalCount);
}
