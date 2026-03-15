using System.Security.Claims;
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
[Route("api/academics/timetable")]
public sealed class TimetableController(IMediator mediator, AcademicsDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<TimetableEntryContract>>> List([FromQuery] Guid schoolId, [FromQuery] Guid? studentUserId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var query = dbContext.TimetableEntries.Where(x => x.SchoolId == schoolId);

        if (IsParentOnly())
        {
            if (!studentUserId.HasValue) return this.ValidationField("studentUserId", "Parent read scope requires studentUserId.");
            if (!SchoolScope.GetLinkedStudentIds(User).Contains(studentUserId.Value)) return Forbid();

            var linkedAudienceIds = await dbContext.AttendanceRecords
                .Where(x => x.SchoolId == schoolId && x.StudentUserId == studentUserId.Value)
                .Select(x => x.AudienceId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(x => linkedAudienceIds.Contains(x.AudienceId));
        }
        else if (IsStudentOnly())
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            if (studentUserId.HasValue && studentUserId.Value != actorUserId) return Forbid();

            var linkedAudienceIds = await dbContext.AttendanceRecords
                .Where(x => x.SchoolId == schoolId && x.StudentUserId == actorUserId)
                .Select(x => x.AudienceId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(x => linkedAudienceIds.Contains(x.AudienceId));
        }
        else if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty) return Forbid();
            query = query.Where(x => x.TeacherUserId == actorUserId);
        }

        return Ok(await query.OrderBy(x => x.DayOfWeek).ThenBy(x => x.StartTime).Select(x => new TimetableEntryContract(x.Id, x.SchoolId, x.SchoolYearId, x.DayOfWeek, x.StartTime, x.EndTime, x.AudienceType, x.AudienceId, x.SubjectId, x.TeacherUserId)).ToListAsync(cancellationToken));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Academics.Api.Auth.SkolioPolicies.SchoolAdministrationOnly)]
    public async Task<ActionResult<TimetableEntryContract>> Create([FromBody] CreateTimetableEntryRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        var result = await mediator.Send(new CreateTimetableEntryCommand(request.SchoolId, request.SchoolYearId, request.DayOfWeek, request.StartTime, request.EndTime, request.AudienceType, request.AudienceId, request.SubjectId, request.TeacherUserId), cancellationToken);
        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
    }

    private bool IsParentOnly()
        => User.IsInRole("Parent") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher");

    private bool IsStudentOnly()
        => User.IsInRole("Student") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator") && !User.IsInRole("Teacher") && !User.IsInRole("Parent");

    private Guid ResolveActorUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var actorUserId) ? actorUserId : Guid.Empty;
    }

    public sealed record CreateTimetableEntryRequest(Guid SchoolId, Guid SchoolYearId, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime, LessonAudienceType AudienceType, Guid AudienceId, Guid SubjectId, Guid TeacherUserId);
}

