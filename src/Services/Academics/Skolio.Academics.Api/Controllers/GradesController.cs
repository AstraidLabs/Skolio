using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.Grades;

namespace Skolio.Academics.Api.Controllers;
[ApiController]
[Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministration)]
[Route("api/academics/grades")]
public sealed class GradesController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<GradeEntryContract>> RecordGrade([FromBody] RecordGradeRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RecordGradeEntryCommand(request.StudentUserId, request.SubjectId, request.GradeValue, request.Note, request.GradedOn), cancellationToken);
        return CreatedAtAction(nameof(RecordGrade), new { id = result.Id }, result);
    }

    public sealed record RecordGradeRequest(Guid StudentUserId, Guid SubjectId, string GradeValue, string Note, DateOnly GradedOn);
}
