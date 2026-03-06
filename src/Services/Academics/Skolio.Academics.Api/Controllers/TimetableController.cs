using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.Timetable;
using Skolio.Academics.Domain.Enums;
using Skolio.Academics.Infrastructure.Persistence;

namespace Skolio.Academics.Api.Controllers;

[ApiController]
[Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.SchoolAdministration)]
[Route("api/academics/timetable")]
public sealed class TimetableController(IMediator mediator, AcademicsDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<TimetableEntryContract>>> List([FromQuery] Guid schoolId, CancellationToken cancellationToken)
        => Ok(await dbContext.TimetableEntries.Where(x => x.SchoolId == schoolId).OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartTime).Select(x => new TimetableEntryContract(x.Id, x.SchoolId, x.SchoolYearId, x.DayOfWeek, x.StartTime, x.EndTime, x.AudienceType, x.AudienceId, x.SubjectId, x.TeacherUserId)).ToListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<TimetableEntryContract>> Create([FromBody] CreateTimetableEntryRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateTimetableEntryCommand(request.SchoolId, request.SchoolYearId, request.DayOfWeek, request.StartTime, request.EndTime, request.AudienceType, request.AudienceId, request.SubjectId, request.TeacherUserId), cancellationToken);
        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
    }

    public sealed record CreateTimetableEntryRequest(Guid SchoolId, Guid SchoolYearId, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, LessonAudienceType AudienceType, Guid AudienceId, Guid SubjectId, Guid TeacherUserId);
}
