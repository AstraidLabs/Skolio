using MediatR;
using Microsoft.AspNetCore.Mvc;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.Timetable;
using Skolio.Academics.Domain.Enums;

namespace Skolio.Academics.Api.Controllers;

[ApiController]
[Route("api/academics/timetable")]
public sealed class TimetableController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TimetableEntryContract>> Create([FromBody] CreateTimetableEntryRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateTimetableEntryCommand(request.SchoolId, request.SchoolYearId, request.DayOfWeek, request.StartTime, request.EndTime, request.AudienceType, request.AudienceId, request.SubjectId, request.TeacherUserId), cancellationToken);
        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
    }

    public sealed record CreateTimetableEntryRequest(Guid SchoolId, Guid SchoolYearId, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, LessonAudienceType AudienceType, Guid AudienceId, Guid SubjectId, Guid TeacherUserId);
}
