using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.Grades;
using Skolio.Academics.Infrastructure.Persistence;

namespace Skolio.Academics.Api.Controllers;
[ApiController]
[Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministration)]
[Route("api/academics/grades")]
public sealed class GradesController(IMediator mediator, AcademicsDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<GradeEntryContract>>> List([FromQuery] Guid studentUserId, CancellationToken cancellationToken)
        => Ok(await dbContext.GradeEntries.Where(x => x.StudentUserId == studentUserId).OrderByDescending(x => x.GradedOn).Select(x => new GradeEntryContract(x.Id, x.StudentUserId, x.SubjectId, x.GradeValue, x.Note, x.GradedOn)).ToListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<GradeEntryContract>> RecordGrade([FromBody] RecordGradeRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RecordGradeEntryCommand(request.StudentUserId, request.SubjectId, request.GradeValue, request.Note, request.GradedOn), cancellationToken);
        return CreatedAtAction(nameof(RecordGrade), new { id = result.Id }, result);
    }

    public sealed record RecordGradeRequest(Guid StudentUserId, Guid SubjectId, string GradeValue, string Note, DateOnly GradedOn);
}
