using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.Homework;
using Skolio.Academics.Infrastructure.Persistence;

namespace Skolio.Academics.Api.Controllers;
[ApiController]
[Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministration)]
[Route("api/academics/homework")]
public sealed class HomeworkController(IMediator mediator, AcademicsDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<HomeworkAssignmentContract>>> List([FromQuery] Guid schoolId, CancellationToken cancellationToken)
        => Ok(await dbContext.HomeworkAssignments.Where(x => x.SchoolId == schoolId).OrderBy(x => x.DueDate).Select(x => new HomeworkAssignmentContract(x.Id, x.SchoolId, x.AudienceId, x.SubjectId, x.Title, x.Instructions, x.DueDate)).ToListAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<HomeworkAssignmentContract>> Assign([FromBody] AssignHomeworkRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AssignHomeworkCommand(request.SchoolId, request.AudienceId, request.SubjectId, request.Title, request.Instructions, request.DueDate), cancellationToken);
        return CreatedAtAction(nameof(Assign), new { id = result.Id }, result);
    }

    public sealed record AssignHomeworkRequest(Guid SchoolId, Guid AudienceId, Guid SubjectId, string Title, string Instructions, DateOnly DueDate);
}
