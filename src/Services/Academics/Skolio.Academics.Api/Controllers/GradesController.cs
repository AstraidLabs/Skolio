using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Academics.Api.Auth;
using Skolio.Academics.Application.Contracts;
using Skolio.Academics.Application.Grades;
using Skolio.Academics.Infrastructure.Persistence;

namespace Skolio.Academics.Api.Controllers;

[ApiController]
[Route("api/academics/grades")]
public sealed class GradesController(IMediator mediator, AcademicsDbContext dbContext, ILogger<GradesController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<IReadOnlyCollection<GradeEntryContract>>> List([FromQuery] Guid schoolId, [FromQuery] Guid studentUserId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var studentInSchool = await dbContext.AttendanceRecords.AnyAsync(x => x.SchoolId == schoolId && x.StudentUserId == studentUserId, cancellationToken);
        if (!studentInSchool) return Ok(Array.Empty<GradeEntryContract>());

        return Ok(await dbContext.GradeEntries.Where(x => x.StudentUserId == studentUserId).OrderByDescending(x => x.GradedOn).Select(x => new GradeEntryContract(x.Id, x.StudentUserId, x.SubjectId, x.GradeValue, x.Note, x.GradedOn)).ToListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<GradeEntryContract>> RecordGrade([FromBody] RecordGradeRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        var result = await mediator.Send(new RecordGradeEntryCommand(request.StudentUserId, request.SubjectId, request.GradeValue, request.Note, request.GradedOn), cancellationToken);
        return CreatedAtAction(nameof(RecordGrade), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/override")]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<GradeEntryContract>> OverrideGrade(Guid id, [FromBody] OverrideGradeRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return BadRequest("Override reason is required.");

        var entity = await dbContext.GradeEntries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.OverrideForPlatformSupport(request.StudentUserId, request.SubjectId, request.GradeValue, request.Note, request.GradedOn);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("academics.grade.override", request.SchoolId, new { request.OverrideReason, entity.Id, request.GradeValue });
        return Ok(new GradeEntryContract(entity.Id, entity.StudentUserId, entity.SubjectId, entity.GradeValue, entity.Note, entity.GradedOn));
    }

    private void Audit(string actionCode, Guid schoolId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} school={SchoolId} payload={Payload}", actionCode, actor, schoolId, payload);
    }

    public sealed record RecordGradeRequest(Guid SchoolId, Guid StudentUserId, Guid SubjectId, string GradeValue, string Note, DateOnly GradedOn);
    public sealed record OverrideGradeRequest(Guid SchoolId, Guid StudentUserId, Guid SubjectId, string GradeValue, string Note, DateOnly GradedOn, string OverrideReason);
}
