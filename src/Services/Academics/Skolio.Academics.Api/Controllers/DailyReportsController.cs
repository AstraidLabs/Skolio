using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.DailyReports;
using Skolio.Academics.Infrastructure.Persistence;

namespace Skolio.Academics.Api.Controllers;

[ApiController]
[Route("api/academics/daily-reports")]
public sealed class DailyReportsController(IMediator mediator, AcademicsDbContext dbContext, ILogger<DailyReportsController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<IReadOnlyCollection<DailyReportContract>>> List([FromQuery] Guid schoolId, CancellationToken cancellationToken)
        => Ok(await dbContext.DailyReports.Where(x => x.SchoolId == schoolId).OrderByDescending(x => x.ReportDate).Select(x => new DailyReportContract(x.Id, x.SchoolId, x.AudienceId, x.ReportDate, x.Summary, x.Notes)).ToListAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<DailyReportContract>> Record([FromBody] RecordDailyReportRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RecordDailyReportCommand(request.SchoolId, request.AudienceId, request.ReportDate, request.Summary, request.Notes), cancellationToken);
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

        Audit("academics.daily-report.override", entity.SchoolId, request.OverrideReason, new { entity.Id, request.ReportDate });
        return Ok(new DailyReportContract(entity.Id, entity.SchoolId, entity.AudienceId, entity.ReportDate, entity.Summary, entity.Notes));
    }

    private void Audit(string actionCode, Guid schoolId, string overrideReason, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} overrideReason={OverrideReason} payload={Payload}", actionCode, actor, schoolId, overrideReason, payload);
    }

    public sealed record RecordDailyReportRequest(Guid SchoolId, Guid AudienceId, DateOnly ReportDate, string Summary, string Notes);
    public sealed record OverrideDailyReportRequest(Guid AudienceId, DateOnly ReportDate, string Summary, string Notes, string OverrideReason);
}