using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.ServiceDefaults.Authorization;
using Skolio.Organization.Application.Contracts;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Api.Controllers;

[ApiController]
[Route("api/organization/role-definitions")]
public sealed class RoleDefinitionsController(OrganizationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<List<RoleDefinitionContract>>> List(CancellationToken cancellationToken)
    {
        var roles = await dbContext.RoleDefinitions
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        return Ok(roles.Select(r => new RoleDefinitionContract(
            r.Id, r.RoleCode, r.TranslationKey, r.ScopeType,
            r.IsBootstrapAllowed, r.IsCreateUserFlowAllowed,
            r.IsUserManagementAllowed, r.SortOrder)).ToList());
    }
}
