using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.Lessons;
using Skolio.Academics.Infrastructure.Persistence;

namespace Skolio.Academics.Api.Controllers;
[ApiController]
[Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministration)]
[Route("api/academics/lessons")]
public sealed class LessonsController(IMediator mediator, AcademicsDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<LessonRecordContract>>> List([FromQuery] Guid schoolId, CancellationToken cancellationToken)
        => Ok(await dbContext.LessonRecords.Join(dbContext.TimetableEntries, lesson => lesson.TimetableEntryId, timetable => timetable.Id, (lesson, timetable) => new { lesson, timetable }).Where(x => x.timetable.SchoolId == schoolId).OrderByDescending(x => x.lesson.LessonDate).Select(x => new LessonRecordContract(x.lesson.Id, x.lesson.TimetableEntryId, x.lesson.LessonDate, x.lesson.Topic, x.lesson.Summary)).ToListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<LessonRecordContract>> Record([FromBody] RecordLessonRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RecordLessonCommand(request.TimetableEntryId, request.LessonDate, request.Topic, request.Summary), cancellationToken);
        return CreatedAtAction(nameof(Record), new { id = result.Id }, result);
    }
    public sealed record RecordLessonRequest(Guid TimetableEntryId, DateOnly LessonDate, string Topic, string Summary);
}
