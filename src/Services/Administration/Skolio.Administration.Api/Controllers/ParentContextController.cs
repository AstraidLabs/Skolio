using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Administration.Api.Auth;
using Skolio.Administration.Infrastructure.Persistence;

namespace Skolio.Administration.Api.Controllers;

[ApiController]
[Route("api/administration/parent-context")]
public sealed class ParentContextController(AdministrationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Administration.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<ParentContextResponse>> Context(CancellationToken cancellationToken)
    {
        if (!SchoolScope.IsParent(User)) return Forbid();

        var scopedSchoolIds = SchoolScope.GetScopedSchoolIds(User).Select(x => x.ToString()).ToList();
        var activeSchoolToggles = await dbContext.FeatureToggles
            .Where(x => x.IsEnabled && EF.Functions.ILike(x.FeatureCode, "school.%"))
            .Select(x => x.FeatureCode)
            .Take(10)
            .ToListAsync(cancellationToken);

        var parentRelevantPolicies = await dbContext.SchoolYearLifecyclePolicies
            .Where(x => scopedSchoolIds.Contains(x.SchoolId.ToString()))
            .OrderBy(x => x.PolicyName)
            .Select(x => x.PolicyName)
            .Take(10)
            .ToListAsync(cancellationToken);

        return Ok(new ParentContextResponse(activeSchoolToggles, parentRelevantPolicies));
    }

    public sealed record ParentContextResponse(IReadOnlyCollection<string> ActiveSchoolFeatureToggles, IReadOnlyCollection<string> SchoolLifecyclePolicySummaries);
}
