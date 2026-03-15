using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Application.Subjects;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Route("api/organization/subjects")]
public sealed class SubjectsController(IMediator mediator, OrganizationDbContext dbContext, ILogger<SubjectsController> logger) : ControllerBase
{
    private const int MaxPageSize = 200;

    [HttpGet]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<PagedResult<SubjectContract>>> List([FromQuery] Guid schoolId, [FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var normalizedPageNumber = Math.Max(pageNumber, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = dbContext.Subjects.Where(x => x.SchoolId == schoolId);
        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();

            var assignedSubjectIds = await dbContext.TeacherAssignments
                .Where(x => x.SchoolId == schoolId && x.TeacherUserId == actorUserId)
                .Where(x => x.SubjectId.HasValue)
                .Select(x => x.SubjectId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(x => assignedSubjectIds.Contains(x.Id));
        }
        else if (IsStudentOnly())
        {
            var scopedSubjectIds = SchoolScope.GetStudentSubjectIds(User);
            if (scopedSubjectIds.Count == 0) return Ok(new PagedResult<SubjectContract>(Array.Empty<SubjectContract>(), normalizedPageNumber, normalizedPageSize, 0));
            query = query.Where(x => scopedSubjectIds.Contains(x.Id));
        }

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
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<SubjectContract>> Detail(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Subjects.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, entity.SchoolId)) return Forbid();

        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();

            var isAssigned = await dbContext.TeacherAssignments.AnyAsync(x => x.SchoolId == entity.SchoolId && x.TeacherUserId == actorUserId && x.SubjectId == entity.Id, cancellationToken);
            if (!isAssigned) return Forbid();
        }
        else if (IsStudentOnly())
        {
            var scopedSubjectIds = SchoolScope.GetStudentSubjectIds(User);
            if (!scopedSubjectIds.Contains(entity.Id)) return Forbid();
        }

        return Ok(new SubjectContract(entity.Id, entity.SchoolId, entity.Code, entity.Name));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.SchoolAdministrationOnly)]
    [ProducesResponseType(typeof(SubjectContract), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSubject([FromBody] CreateSubjectRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        var contract = await mediator.Send(new CreateSubjectCommand(request.SchoolId, request.Code, request.Name), cancellationToken);
        Audit("organization.subject.created", request.SchoolId, new { contract.Id, request.Code, request.Name });
        return CreatedAtAction(nameof(Detail), new { id = contract.Id }, contract);
    }

    [HttpPut("{id:guid}/override")]
    [Authorize(Policy = Skolio.Organization.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<SubjectContract>> OverrideSubject(Guid id, [FromBody] OverrideSubjectRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return this.ValidationField("overrideReason", "Override reason is required.");

        var entity = await dbContext.Subjects.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.OverrideForPlatformSupport(request.Code, request.Name);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("organization.subject.override", entity.SchoolId, new { request.OverrideReason, entity.Id, request.Code, request.Name });
        return Ok(new SubjectContract(entity.Id, entity.SchoolId, entity.Code, entity.Name));
    }

    private Guid ResolveActorUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var actorUserId) ? actorUserId : Guid.Empty;
    }

    private void Audit(string actionCode, Guid schoolId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} payload={Payload}", actionCode, actor, schoolId, payload);
    }

    public sealed record CreateSubjectRequest(Guid SchoolId, string Code, string Name);
    public sealed record OverrideSubjectRequest(string Code, string Name, string OverrideReason);
    public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int PageNumber, int PageSize, int TotalCount);

    private bool IsStudentOnly()
        => User.IsInRole("Student") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher") && !User.IsInRole("Parent");
}


