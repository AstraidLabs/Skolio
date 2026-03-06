using MediatR;
using Microsoft.AspNetCore.Mvc;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.DailyReports;

namespace Skolio.Academics.Api.Controllers;
[ApiController]
[Route("api/academics/daily-reports")]
public sealed class DailyReportsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<DailyReportContract>> Record([FromBody] RecordDailyReportRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RecordDailyReportCommand(request.SchoolId, request.AudienceId, request.ReportDate, request.Summary, request.Notes), cancellationToken);
        return CreatedAtAction(nameof(Record), new { id = result.Id }, result);
    }

    public sealed record RecordDailyReportRequest(Guid SchoolId, Guid AudienceId, DateOnly ReportDate, string Summary, string Notes);
}
