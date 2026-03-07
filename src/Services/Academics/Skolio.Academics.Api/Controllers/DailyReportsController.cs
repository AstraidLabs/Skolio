using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Academics.Api.Auth;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.DailyReports;
using Skolio.Academics.Infrastructure.Persistence;

namespace Skolio.Academics.Api.Controllers;

[ApiController]
[Route("api/academics/daily-reports")]
public sealed class DailyReportsController(IMediator mediator, AcademicsDbContext dbContext, ILogger<DailyReportsController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<DailyReportContract>>> List([FromQuery] Guid schoolId, [FromQuery] Guid? audienceId, [FromQuery] Guid? studentUserId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var query = dbContext.DailyReports.Where(x => x.SchoolId == schoolId);
        if (audienceId.HasValue)
        {
            query = query.Where(x => x.AudienceId == audienceId.Value);
        }

        if (IsParentOnly())
        {
            if (!studentUserId.HasValue) return BadRequest("Parent read scope requires studentUserId.");
            if (!SchoolScope.GetLinkedStudentIds(User).Contains(studentUserId.Value)) return Forbid();

            var linkedAudienceIds = await dbContext.AttendanceRecords
                .Where(x => x.SchoolId == schoolId && x.StudentUserId == studentUserId.Value)
                .Select(x => x.AudienceId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(x => linkedAudienceIds.Contains(x.AudienceId));
        }
        else if (IsStudentOnly())
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            if (studentUserId.HasValue && studentUserId.Value != actorUserId) return Forbid();

            var linkedAudienceIds = await dbContext.AttendanceRecords
                .Where(x => x.SchoolId == schoolId && x.StudentUserId == actorUserId)
                .Select(x => x.AudienceId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(x => linkedAudienceIds.Contains(x.AudienceId));
        }
        else if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            if (!audienceId.HasValue) return BadRequest("Teacher read scope requires audienceId.");

            var hasTeachingContext = await dbContext.TimetableEntries.AnyAsync(x => x.SchoolId == schoolId && x.TeacherUserId == actorUserId && x.AudienceId == audienceId.Value, cancellationToken);
            if (!hasTeachingContext) return Forbid();
        }

        return Ok(await query.OrderByDescending(x => x.ReportDate).Select(x => new DailyReportContract(x.Id, x.SchoolId, x.AudienceId, x.ReportDate, x.Summary, x.Notes)).ToListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<DailyReportContract>> Record([FromBody] RecordDailyReportRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();

            var hasTeachingContext = await dbContext.TimetableEntries.AnyAsync(x => x.SchoolId == request.SchoolId && x.TeacherUserId == actorUserId && x.AudienceId == request.AudienceId, cancellationToken);
            if (!hasTeachingContext) return Forbid();
        }

        var result = await mediator.Send(new RecordDailyReportCommand(request.SchoolId, request.AudienceId, request.ReportDate, request.Summary, request.Notes), cancellationToken);
        Audit("academics.daily-report.changed", request.SchoolId, new { operation = "create", result.Id, request.AudienceId, request.ReportDate });
        return CreatedAtAction(nameof(Record), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/override")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<DailyReportContract>> OverrideDailyReport(Guid id, [FromBody] OverrideDailyReportRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return BadRequest("Override reason is required.");

        var entity = await dbContext.DailyReports.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.OverrideForPlatformSupport(request.AudienceId, request.ReportDate, request.Summary, request.Notes);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("academics.daily-report.override", entity.SchoolId, new { request.OverrideReason, entity.Id, request.ReportDate });
        return Ok(new DailyReportContract(entity.Id, entity.SchoolId, entity.AudienceId, entity.ReportDate, entity.Summary, entity.Notes));
    }

    private bool IsParentOnly()
        => User.IsInRole("Parent") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher");

    private bool IsStudentOnly()
        => User.IsInRole("Student") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher") && !User.IsInRole("Parent");

    private Guid ResolveActorUserId() => SchoolScope.ResolveActorUserId(User);

    private void Audit(string actionCode, Guid schoolId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} payload={Payload}", actionCode, actor, schoolId, payload);
    }

    public sealed record RecordDailyReportRequest(Guid SchoolId, Guid AudienceId, DateOnly ReportDate, string Summary, string Notes);
    public sealed record OverrideDailyReportRequest(Guid AudienceId, DateOnly ReportDate, string Summary, string Notes, string OverrideReason);
}
