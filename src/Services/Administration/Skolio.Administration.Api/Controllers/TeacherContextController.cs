using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Administration.Infrastructure.Persistence;

namespace Skolio.Administration.Api.Controllers;

[ApiController]
[Route("api/administration/teacher-context")]
public sealed class TeacherContextController(AdministrationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Administration.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<TeacherContextContract>> Summary(CancellationToken cancellationToken)
    {
        if (!User.IsInRole("Teacher")) return Forbid();

        var actorClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(actorClaim, out var actorUserId)) return Forbid();

        var recentAudit = await dbContext.AuditLogEntries
            .Where(x => x.ActorUserId == actorUserId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(15)
            .Select(x => x.ActionCode)
            .ToListAsync(cancellationToken);

        var schoolScopedLifecycleHints = await dbContext.SchoolYearLifecyclePolicies
            .Where(x => x.Status == Skolio.Administration.Domain.Enums.PolicyStatus.Active)
            .OrderBy(x => x.PolicyName)
            .Take(10)
            .Select(x => x.PolicyName)
            .ToListAsync(cancellationToken);

        return Ok(new TeacherContextContract(recentAudit, schoolScopedLifecycleHints));
    }

    public sealed record TeacherContextContract(IReadOnlyCollection<string> RecentTeacherAuditActions, IReadOnlyCollection<string> ActiveLifecycleHints);
}
