using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Administration.Application.AuditLogs;
using Skolio.Administration.Application.Contracts;
using Skolio.Administration.Application.FeatureToggles;
using Skolio.Administration.Application.HousekeepingPolicies;
using Skolio.Administration.Application.SchoolYearPolicies;
using Skolio.Administration.Application.SystemSettings;
using Skolio.Administration.Infrastructure.Persistence;

namespace Skolio.Administration.Api.Controllers;

[ApiController]
[Authorize(Policy = Skolio.Administration.Api.Auth.SkolioPolicies.PlatformAdministration)]
[Route("api/administration")]
public sealed class AdministrationController(IMediator mediator, AdministrationDbContext dbContext) : ControllerBase
{
    [HttpGet("system-settings")]
    public async Task<ActionResult<IReadOnlyCollection<SystemSettingContract>>> Settings(CancellationToken cancellationToken)
        => Ok(await dbContext.SystemSettings.OrderBy(x => x.Key).Select(x => new SystemSettingContract(x.Id, x.Key, x.Value, x.IsSensitive)).ToListAsync(cancellationToken));

    [HttpPut("system-settings/{id:guid}")]
    public async Task<ActionResult<SystemSettingContract>> UpdateSetting(Guid id, [FromBody] UpdateSettingRequest request, CancellationToken cancellationToken)
    {
        var current = await dbContext.SystemSettings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (current is null) return NotFound();
        return Ok(await mediator.Send(new ChangeSystemSettingCommand(current.Key, request.Value, request.IsSensitive), cancellationToken));
    }

    [HttpGet("feature-toggles")]
    public async Task<ActionResult<IReadOnlyCollection<FeatureToggleContract>>> Toggles(CancellationToken cancellationToken)
        => Ok(await dbContext.FeatureToggles.OrderBy(x => x.FeatureCode).Select(x => new FeatureToggleContract(x.Id, x.FeatureCode, x.IsEnabled)).ToListAsync(cancellationToken));

    [HttpPut("feature-toggles/{id:guid}")]
    public async Task<ActionResult<FeatureToggleContract>> UpdateToggle(Guid id, [FromBody] UpdateToggleRequest request, CancellationToken cancellationToken)
    {
        var current = await dbContext.FeatureToggles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (current is null) return NotFound();
        return Ok(await mediator.Send(new ChangeFeatureToggleCommand(current.FeatureCode, request.IsEnabled), cancellationToken));
    }

    [HttpGet("audit-log")]
    public async Task<ActionResult<IReadOnlyCollection<AuditLogEntryContract>>> AuditLog(CancellationToken cancellationToken)
        => Ok(await dbContext.AuditLogEntries.OrderByDescending(x => x.CreatedAtUtc).Take(200).Select(x => new AuditLogEntryContract(x.Id, x.ActorUserId, x.ActionCode, x.Payload, x.CreatedAtUtc)).ToListAsync(cancellationToken));

    [HttpGet("school-year-policies")]
    public async Task<ActionResult<IReadOnlyCollection<SchoolYearLifecyclePolicyContract>>> SchoolYearPolicies(CancellationToken cancellationToken)
        => Ok(await dbContext.SchoolYearLifecyclePolicies.OrderBy(x => x.PolicyName).Select(x => new SchoolYearLifecyclePolicyContract(x.Id, x.SchoolId, x.PolicyName, x.ClosureGraceDays, x.Status)).ToListAsync(cancellationToken));

    [HttpPut("school-year-policies/{id:guid}")]
    public async Task<ActionResult<SchoolYearLifecyclePolicyContract>> UpdateSchoolYearPolicy(Guid id, [FromBody] UpdateSchoolYearPolicyRequest request, CancellationToken cancellationToken)
    {
        var current = await dbContext.SchoolYearLifecyclePolicies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (current is null) return NotFound();
        return Ok(await mediator.Send(new ManageSchoolYearLifecyclePolicyCommand(current.SchoolId, current.PolicyName, request.ClosureGraceDays, request.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)), cancellationToken));
    }

    [HttpGet("housekeeping-policies")]
    public async Task<ActionResult<IReadOnlyCollection<HousekeepingPolicyContract>>> HousekeepingPolicies(CancellationToken cancellationToken)
        => Ok(await dbContext.HousekeepingPolicies.OrderBy(x => x.PolicyName).Select(x => new HousekeepingPolicyContract(x.Id, x.PolicyName, x.RetentionDays, x.Status)).ToListAsync(cancellationToken));

    [HttpPut("housekeeping-policies/{id:guid}")]
    public async Task<ActionResult<HousekeepingPolicyContract>> UpdateHousekeepingPolicy(Guid id, [FromBody] UpdateHousekeepingPolicyRequest request, CancellationToken cancellationToken)
    {
        var current = await dbContext.HousekeepingPolicies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (current is null) return NotFound();
        return Ok(await mediator.Send(new ManageHousekeepingPolicyCommand(current.PolicyName, request.RetentionDays, request.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)), cancellationToken));
    }

    public sealed record UpdateSettingRequest(string Value, bool IsSensitive);
    public sealed record UpdateToggleRequest(bool IsEnabled);
    public sealed record UpdateSchoolYearPolicyRequest(int ClosureGraceDays, string Status);
    public sealed record UpdateHousekeepingPolicyRequest(int RetentionDays, string Status);

    public sealed record WriteAuditLogRequest(Guid ActorUserId, string ActionCode, string Payload);
}
