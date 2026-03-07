using System.Security.Claims;
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
    private const int MaxPageSize = 200;

    [HttpGet("system-settings")]
    public async Task<ActionResult<IReadOnlyCollection<SystemSettingContract>>> Settings(CancellationToken cancellationToken)
        => Ok(await dbContext.SystemSettings.OrderBy(x => x.Key).Select(x => new SystemSettingContract(x.Id, x.Key, x.Value, x.IsSensitive)).ToListAsync(cancellationToken));

    [HttpPut("system-settings/{id:guid}")]
    public async Task<ActionResult<SystemSettingContract>> UpdateSetting(Guid id, [FromBody] UpdateSettingRequest request, CancellationToken cancellationToken)
    {
        var current = await dbContext.SystemSettings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (current is null) return NotFound();

        var result = await mediator.Send(new ChangeSystemSettingCommand(current.Key, request.Value, request.IsSensitive), cancellationToken);
        await WriteAudit("administration.system-setting.changed", new { id, current.Key, request.IsSensitive, request.Value }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("feature-toggles")]
    public async Task<ActionResult<IReadOnlyCollection<FeatureToggleContract>>> Toggles(CancellationToken cancellationToken)
        => Ok(await dbContext.FeatureToggles.OrderBy(x => x.FeatureCode).Select(x => new FeatureToggleContract(x.Id, x.FeatureCode, x.IsEnabled)).ToListAsync(cancellationToken));

    [HttpPut("feature-toggles/{id:guid}")]
    public async Task<ActionResult<FeatureToggleContract>> UpdateToggle(Guid id, [FromBody] UpdateToggleRequest request, CancellationToken cancellationToken)
    {
        var current = await dbContext.FeatureToggles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (current is null) return NotFound();

        var result = await mediator.Send(new ChangeFeatureToggleCommand(current.FeatureCode, request.IsEnabled), cancellationToken);
        await WriteAudit("administration.feature-toggle.changed", new { id, current.FeatureCode, request.IsEnabled }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("audit-log")]
    public async Task<ActionResult<PagedResult<AuditLogEntryContract>>> AuditLog(
        [FromQuery] Guid? actorUserId,
        [FromQuery] string? actionCode,
        [FromQuery] DateTimeOffset? fromUtc,
        [FromQuery] DateTimeOffset? toUtc,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var normalizedPageNumber = Math.Max(pageNumber, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = dbContext.AuditLogEntries.AsQueryable();
        if (actorUserId.HasValue)
        {
            query = query.Where(x => x.ActorUserId == actorUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(actionCode))
        {
            query = query.Where(x => x.ActionCode == actionCode);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(x => x.CreatedAtUtc <= toUtc.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc)
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(x => new AuditLogEntryContract(x.Id, x.ActorUserId, x.ActionCode, x.Payload, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<AuditLogEntryContract>(items, normalizedPageNumber, normalizedPageSize, totalCount));
    }

    [HttpGet("audit-log/{id:guid}")]
    public async Task<ActionResult<AuditLogEntryContract>> AuditLogDetail(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.AuditLogEntries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null
            ? NotFound()
            : Ok(new AuditLogEntryContract(entity.Id, entity.ActorUserId, entity.ActionCode, entity.Payload, entity.CreatedAtUtc));
    }

    [HttpGet("school-year-policies")]
    public async Task<ActionResult<IReadOnlyCollection<SchoolYearLifecyclePolicyContract>>> SchoolYearPolicies(CancellationToken cancellationToken)
        => Ok(await dbContext.SchoolYearLifecyclePolicies.OrderBy(x => x.PolicyName).Select(x => new SchoolYearLifecyclePolicyContract(x.Id, x.SchoolId, x.PolicyName, x.ClosureGraceDays, x.Status)).ToListAsync(cancellationToken));

    [HttpPut("school-year-policies/{id:guid}")]
    public async Task<ActionResult<SchoolYearLifecyclePolicyContract>> UpdateSchoolYearPolicy(Guid id, [FromBody] UpdateSchoolYearPolicyRequest request, CancellationToken cancellationToken)
    {
        var current = await dbContext.SchoolYearLifecyclePolicies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (current is null) return NotFound();

        var result = await mediator.Send(new ManageSchoolYearLifecyclePolicyCommand(current.SchoolId, current.PolicyName, request.ClosureGraceDays, request.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)), cancellationToken);
        await WriteAudit("administration.school-year-policy.changed", new { id, current.PolicyName, request.ClosureGraceDays, request.Status }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("housekeeping-policies")]
    public async Task<ActionResult<IReadOnlyCollection<HousekeepingPolicyContract>>> HousekeepingPolicies(CancellationToken cancellationToken)
        => Ok(await dbContext.HousekeepingPolicies.OrderBy(x => x.PolicyName).Select(x => new HousekeepingPolicyContract(x.Id, x.PolicyName, x.RetentionDays, x.Status)).ToListAsync(cancellationToken));

    [HttpPut("housekeeping-policies/{id:guid}")]
    public async Task<ActionResult<HousekeepingPolicyContract>> UpdateHousekeepingPolicy(Guid id, [FromBody] UpdateHousekeepingPolicyRequest request, CancellationToken cancellationToken)
    {
        var current = await dbContext.HousekeepingPolicies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (current is null) return NotFound();

        var result = await mediator.Send(new ManageHousekeepingPolicyCommand(current.PolicyName, request.RetentionDays, request.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)), cancellationToken);
        await WriteAudit("administration.housekeeping-policy.changed", new { id, current.PolicyName, request.RetentionDays, request.Status }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("operational-summary")]
    public async Task<ActionResult<OperationalSummaryResponse>> OperationalSummary(CancellationToken cancellationToken)
    {
        var recentAuditCount = await dbContext.AuditLogEntries.CountAsync(x => x.CreatedAtUtc >= DateTimeOffset.UtcNow.AddDays(-7), cancellationToken);
        var enabledFeatureToggles = await dbContext.FeatureToggles.CountAsync(x => x.IsEnabled, cancellationToken);
        var activeLifecyclePolicies = await dbContext.SchoolYearLifecyclePolicies.CountAsync(x => x.Status == Skolio.Administration.Domain.Enums.PolicyStatus.Active, cancellationToken);
        var activeHousekeepingPolicies = await dbContext.HousekeepingPolicies.CountAsync(x => x.Status == Skolio.Administration.Domain.Enums.PolicyStatus.Active, cancellationToken);
        var latestAudit = await dbContext.AuditLogEntries.OrderByDescending(x => x.CreatedAtUtc).Take(5).Select(x => x.ActionCode).ToListAsync(cancellationToken);

        return Ok(new OperationalSummaryResponse(recentAuditCount, enabledFeatureToggles, activeLifecyclePolicies, activeHousekeepingPolicies, latestAudit));
    }

    private async Task WriteAudit(string actionCode, object payload, CancellationToken cancellationToken)
    {
        var actorClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(actorClaim, out var actorUserId))
        {
            actorUserId = Guid.Empty;
        }

        await mediator.Send(new WriteAuditLogEntryCommand(actorUserId, actionCode, System.Text.Json.JsonSerializer.Serialize(payload)), cancellationToken);
    }

    public sealed record UpdateSettingRequest(string Value, bool IsSensitive);
    public sealed record UpdateToggleRequest(bool IsEnabled);
    public sealed record UpdateSchoolYearPolicyRequest(int ClosureGraceDays, string Status);
    public sealed record UpdateHousekeepingPolicyRequest(int RetentionDays, string Status);
    public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int PageNumber, int PageSize, int TotalCount);
    public sealed record OperationalSummaryResponse(int RecentAuditCount, int EnabledFeatureToggles, int ActiveLifecyclePolicies, int ActiveHousekeepingPolicies, IReadOnlyCollection<string> LatestAuditActions);
}