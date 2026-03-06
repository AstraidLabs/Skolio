using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.DailyReports;
using Skolio.Academics.Infrastructure.Persistence;

namespace Skolio.Academics.Api.Controllers;
[ApiController]
[Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministration)]
[Route("api/academics/daily-reports")]
public sealed class DailyReportsController(IMediator mediator, AcademicsDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<DailyReportContract>>> List([FromQuery] Guid schoolId, CancellationToken cancellationToken)
        => Ok(await dbContext.DailyReports.Where(x => x.SchoolId == schoolId).OrderByDescending(x => x.ReportDate).Select(x => new DailyReportContract(x.Id, x.SchoolId, x.AudienceId, x.ReportDate, x.Summary, x.Notes)).ToListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<DailyReportContract>> Record([FromBody] RecordDailyReportRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RecordDailyReportCommand(request.SchoolId, request.AudienceId, request.ReportDate, request.Summary, request.Notes), cancellationToken);
        return CreatedAtAction(nameof(Record), new { id = result.Id }, result);
    }

    public sealed record RecordDailyReportRequest(Guid SchoolId, Guid AudienceId, DateOnly ReportDate, string Summary, string Notes);
}
