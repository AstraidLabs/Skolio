using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Administration.Infrastructure.Persistence;

namespace Skolio.Administration.Api.Controllers;

[ApiController]
[Route("api/administration/student-context")]
public sealed class StudentContextController(AdministrationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Administration.Api.Auth.SkolioPolicies.StudentSelfService)]
    public async Task<ActionResult<StudentContextResponse>> Context(CancellationToken cancellationToken)
    {
        if (!SchoolScope.IsStudent(User)) return Forbid();

        var actorClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(actorClaim, out var actorUserId)) return Forbid();

        var scopedSchoolIds = SchoolScope.GetScopedSchoolIds(User).ToList();
        if (scopedSchoolIds.Count == 0)
        {
            return Ok(new StudentContextResponse(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>()));
        }

        var activeSchoolToggles = await dbContext.FeatureToggles
            .Where(x => x.IsEnabled && EF.Functions.ILike(x.FeatureCode, "school.%"))
            .OrderBy(x => x.FeatureCode)
            .Select(x => x.FeatureCode)
            .Take(12)
            .ToListAsync(cancellationToken);

        var lifecycleSummaries = await dbContext.SchoolYearLifecyclePolicies
            .Where(x => scopedSchoolIds.Contains(x.SchoolId))
            .OrderBy(x => x.PolicyName)
            .Select(x => x.PolicyName)
            .Take(12)
            .ToListAsync(cancellationToken);

        var ownAudit = await dbContext.AuditLogEntries
            .Where(x => x.ActorUserId == actorUserId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => x.ActionCode)
            .Take(12)
            .ToListAsync(cancellationToken);

        return Ok(new StudentContextResponse(activeSchoolToggles, lifecycleSummaries, ownAudit));
    }

    public sealed record StudentContextResponse(
        IReadOnlyCollection<string> ActiveSchoolFeatureToggles,
        IReadOnlyCollection<string> SchoolLifecyclePolicySummaries,
        IReadOnlyCollection<string> RecentStudentAuditActions);
}
