using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.Lessons;

namespace Skolio.Academics.Api.Controllers;
[ApiController]
[Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministration)]
[Route("api/academics/lessons")]
public sealed class LessonsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<LessonRecordContract>> Record([FromBody] RecordLessonRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RecordLessonCommand(request.TimetableEntryId, request.LessonDate, request.Topic, request.Summary), cancellationToken);
        return CreatedAtAction(nameof(Record), new { id = result.Id }, result);
    }
    public sealed record RecordLessonRequest(Guid TimetableEntryId, DateOnly LessonDate, string Topic, string Summary);
}
