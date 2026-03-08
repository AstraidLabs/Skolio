using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Administration.Api.Auth;
using Skolio.Administration.Application.AuditLogs;
using Skolio.Administration.Application.Contracts;
using Skolio.Administration.Application.FeatureToggles;
using Skolio.Administration.Application.HousekeepingPolicies;
using Skolio.Administration.Application.SchoolYearPolicies;
using Skolio.Administration.Application.SystemSettings;
using Skolio.Administration.Domain.Entities;
using Skolio.Administration.Infrastructure.Persistence;

namespace Skolio.Administration.Api.Controllers;

[ApiController]
[Authorize(Policy = Skolio.Administration.Api.Auth.SkolioPolicies.SharedAdministration)]
[Route("api/administration")]
public sealed class AdministrationController(IMediator mediator, AdministrationDbContext dbContext) : ControllerBase
{
    private const int MaxPageSize = 200;

    [HttpGet("system-settings")]
    public async Task<ActionResult<IReadOnlyCollection<SystemSettingContract>>> Settings(CancellationToken cancellationToken)
    {
        var query = dbContext.SystemSettings.AsQueryable();
        if (!SchoolScope.IsPlatformAdministrator(User))
        {
            query = query.Where(x => !x.IsSensitive && EF.Functions.ILike(x.Key, "school.%"));
        }

        return Ok(await query.OrderBy(x => x.Key).Select(x => new SystemSettingContract(x.Id, x.Key, x.Value, x.IsSensitive)).ToListAsync(cancellationToken));
    }

    [HttpPut("system-settings/{id:guid}")]
    [Authorize(Policy = Skolio.Administration.Api.Auth.SkolioPolicies.PlatformAdministration)]
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
    {
        var query = dbContext.FeatureToggles.AsQueryable();
        if (!SchoolScope.IsPlatformAdministrator(User))
        {
            query = query.Where(x => EF.Functions.ILike(x.FeatureCode, "school.%"));
        }

        return Ok(await query.OrderBy(x => x.FeatureCode).Select(x => new FeatureToggleContract(x.Id, x.FeatureCode, x.IsEnabled)).ToListAsync(cancellationToken));
    }

    [HttpPut("feature-toggles/{id:guid}")]
    public async Task<ActionResult<FeatureToggleContract>> UpdateToggle(Guid id, [FromBody] UpdateToggleRequest request, CancellationToken cancellationToken)
    {
        var current = await dbContext.FeatureToggles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (current is null) return NotFound();

        if (!SchoolScope.IsPlatformAdministrator(User) && !current.FeatureCode.StartsWith("school.", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var result = await mediator.Send(new ChangeFeatureToggleCommand(current.FeatureCode, request.IsEnabled), cancellationToken);
        await WriteAudit("administration.feature-toggle.changed", new { id, current.FeatureCode, request.IsEnabled }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("audit-log")]
    public async Task<ActionResult<PagedResult<AuditLogEntryContract>>> AuditLog([FromQuery] Guid? actorUserId, [FromQuery] string? actionCode, [FromQuery] DateTimeOffset? fromUtc, [FromQuery] DateTimeOffset? toUtc, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
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

        var itemsQuery = query.OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new AuditLogEntryContract(x.Id, x.ActorUserId, x.ActionCode, x.Payload, x.CreatedAtUtc));

        List<AuditLogEntryContract> filtered;
        if (SchoolScope.IsPlatformAdministrator(User))
        {
            filtered = await itemsQuery.ToListAsync(cancellationToken);
        }
        else
        {
            var scopedSchoolIds = SchoolScope.GetScopedSchoolIds(User).Select(x => x.ToString()).ToList();
            filtered = await itemsQuery.Where(x => scopedSchoolIds.Any(id => x.Payload.Contains(id))).ToListAsync(cancellationToken);
        }

        var totalCount = filtered.Count;
        var items = filtered.Skip((normalizedPageNumber - 1) * normalizedPageSize).Take(normalizedPageSize).ToList();
        return Ok(new PagedResult<AuditLogEntryContract>(items, normalizedPageNumber, normalizedPageSize, totalCount));
    }

    [HttpGet("audit-log/{id:guid}")]
    public async Task<ActionResult<AuditLogEntryContract>> AuditLogDetail(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.AuditLogEntries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        if (!SchoolScope.IsPlatformAdministrator(User))
        {
            var scopedSchoolIds = SchoolScope.GetScopedSchoolIds(User).Select(x => x.ToString());
            if (!scopedSchoolIds.Any(schoolId => entity.Payload.Contains(schoolId, StringComparison.OrdinalIgnoreCase))) return Forbid();
        }

        return Ok(new AuditLogEntryContract(entity.Id, entity.ActorUserId, entity.ActionCode, entity.Payload, entity.CreatedAtUtc));
    }

    [HttpGet("school-year-policies")]
    public async Task<ActionResult<IReadOnlyCollection<SchoolYearLifecyclePolicyContract>>> SchoolYearPolicies(CancellationToken cancellationToken)
    {
        var query = dbContext.SchoolYearLifecyclePolicies.AsQueryable();
        if (!SchoolScope.IsPlatformAdministrator(User))
        {
            var scopedSchoolIds = SchoolScope.GetScopedSchoolIds(User);
            query = query.Where(x => scopedSchoolIds.Contains(x.SchoolId));
        }

        return Ok(await query.OrderBy(x => x.PolicyName).Select(x => new SchoolYearLifecyclePolicyContract(x.Id, x.SchoolId, x.PolicyName, x.ClosureGraceDays, x.Status)).ToListAsync(cancellationToken));
    }

    [HttpPut("school-year-policies/{id:guid}")]
    public async Task<ActionResult<SchoolYearLifecyclePolicyContract>> UpdateSchoolYearPolicy(Guid id, [FromBody] UpdateSchoolYearPolicyRequest request, CancellationToken cancellationToken)
    {
        var current = await dbContext.SchoolYearLifecyclePolicies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (current is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, current.SchoolId)) return Forbid();

        var result = await mediator.Send(new ManageSchoolYearLifecyclePolicyCommand(current.SchoolId, current.PolicyName, request.ClosureGraceDays, request.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)), cancellationToken);
        await WriteAudit("administration.school-year-policy.changed", new { id, current.PolicyName, request.ClosureGraceDays, request.Status, current.SchoolId }, cancellationToken);
        return Ok(result);
    }

    [HttpGet("housekeeping-policies")]
    public async Task<ActionResult<IReadOnlyCollection<HousekeepingPolicyContract>>> HousekeepingPolicies(CancellationToken cancellationToken)
    {
        var query = dbContext.HousekeepingPolicies.AsQueryable();
        if (!SchoolScope.IsPlatformAdministrator(User))
        {
            query = query.Where(x => EF.Functions.ILike(x.PolicyName, "school.%"));
        }

        return Ok(await query.OrderBy(x => x.PolicyName).Select(x => new HousekeepingPolicyContract(x.Id, x.PolicyName, x.RetentionDays, x.Status)).ToListAsync(cancellationToken));
    }

    [HttpPut("housekeeping-policies/{id:guid}")]
    [Authorize(Policy = Skolio.Administration.Api.Auth.SkolioPolicies.PlatformAdministration)]
    public async Task<ActionResult<HousekeepingPolicyContract>> UpdateHousekeepingPolicy(Guid id, [FromBody] UpdateHousekeepingPolicyRequest request, CancellationToken cancellationToken)
    {
        var current = await dbContext.HousekeepingPolicies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (current is null) return NotFound();

        var result = await mediator.Send(new ManageHousekeepingPolicyCommand(current.PolicyName, request.RetentionDays, request.Status.Equals("Active", StringComparison.OrdinalIgnoreCase)), cancellationToken);
        await WriteAudit("administration.housekeeping-policy.changed", new { id, current.PolicyName, request.RetentionDays, request.Status }, cancellationToken);
        return Ok(result);
    }

    [HttpPost("system-settings")]
    [Authorize(Policy = SkolioPolicies.PlatformAdministration)]
    public async Task<ActionResult<SystemSettingContract>> CreateSetting([FromBody] CreateSettingRequest request, CancellationToken cancellationToken)
    {
        if (await dbContext.SystemSettings.AnyAsync(x => x.Key == request.Key, cancellationToken))
            return Conflict(new { message = $"Setting with key '{request.Key}' already exists." });

        var entity = SystemSetting.Create(Guid.NewGuid(), request.Key, request.Value, request.IsSensitive);
        await dbContext.SystemSettings.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await WriteAudit("administration.system-setting.created", new { entity.Id, request.Key, request.Value, request.IsSensitive }, cancellationToken);
        return Created($"/api/administration/system-settings", new SystemSettingContract(entity.Id, entity.Key, entity.Value, entity.IsSensitive));
    }

    [HttpDelete("system-settings/{id:guid}")]
    [Authorize(Policy = SkolioPolicies.PlatformAdministration)]
    public async Task<ActionResult> DeleteSetting(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.SystemSettings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        dbContext.SystemSettings.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await WriteAudit("administration.system-setting.deleted", new { id, entity.Key }, cancellationToken);
        return NoContent();
    }

    [HttpPost("feature-toggles")]
    [Authorize(Policy = SkolioPolicies.PlatformAdministration)]
    public async Task<ActionResult<FeatureToggleContract>> CreateToggle([FromBody] CreateToggleRequest request, CancellationToken cancellationToken)
    {
        if (await dbContext.FeatureToggles.AnyAsync(x => x.FeatureCode == request.FeatureCode, cancellationToken))
            return Conflict(new { message = $"Feature toggle '{request.FeatureCode}' already exists." });

        var entity = FeatureToggle.Create(Guid.NewGuid(), request.FeatureCode, request.IsEnabled);
        await dbContext.FeatureToggles.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await WriteAudit("administration.feature-toggle.created", new { entity.Id, request.FeatureCode, request.IsEnabled }, cancellationToken);
        return Created($"/api/administration/feature-toggles", new FeatureToggleContract(entity.Id, entity.FeatureCode, entity.IsEnabled));
    }

    [HttpDelete("feature-toggles/{id:guid}")]
    [Authorize(Policy = SkolioPolicies.PlatformAdministration)]
    public async Task<ActionResult> DeleteToggle(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.FeatureToggles.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        dbContext.FeatureToggles.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await WriteAudit("administration.feature-toggle.deleted", new { id, entity.FeatureCode }, cancellationToken);
        return NoContent();
    }

    [HttpPost("school-year-policies")]
    [Authorize(Policy = SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult<SchoolYearLifecyclePolicyContract>> CreateSchoolYearPolicy([FromBody] CreateSchoolYearPolicyRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        var entity = SchoolYearLifecyclePolicy.Create(Guid.NewGuid(), request.SchoolId, request.PolicyName, request.ClosureGraceDays);
        if (request.Activate) entity.Activate();
        await dbContext.SchoolYearLifecyclePolicies.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await WriteAudit("administration.school-year-policy.created", new { entity.Id, request.SchoolId, request.PolicyName, request.ClosureGraceDays, request.Activate }, cancellationToken);
        return Created($"/api/administration/school-year-policies", new SchoolYearLifecyclePolicyContract(entity.Id, entity.SchoolId, entity.PolicyName, entity.ClosureGraceDays, entity.Status));
    }

    [HttpDelete("school-year-policies/{id:guid}")]
    [Authorize(Policy = SkolioPolicies.SharedAdministration)]
    public async Task<ActionResult> DeleteSchoolYearPolicy(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.SchoolYearLifecyclePolicies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, entity.SchoolId)) return Forbid();

        dbContext.SchoolYearLifecyclePolicies.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await WriteAudit("administration.school-year-policy.deleted", new { id, entity.PolicyName, entity.SchoolId }, cancellationToken);
        return NoContent();
    }

    [HttpPost("housekeeping-policies")]
    [Authorize(Policy = SkolioPolicies.PlatformAdministration)]
    public async Task<ActionResult<HousekeepingPolicyContract>> CreateHousekeepingPolicy([FromBody] CreateHousekeepingPolicyRequest request, CancellationToken cancellationToken)
    {
        var entity = HousekeepingPolicy.Create(Guid.NewGuid(), request.PolicyName, request.RetentionDays);
        if (request.Activate) entity.Activate();
        await dbContext.HousekeepingPolicies.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await WriteAudit("administration.housekeeping-policy.created", new { entity.Id, request.PolicyName, request.RetentionDays, request.Activate }, cancellationToken);
        return Created($"/api/administration/housekeeping-policies", new HousekeepingPolicyContract(entity.Id, entity.PolicyName, entity.RetentionDays, entity.Status));
    }

    [HttpDelete("housekeeping-policies/{id:guid}")]
    [Authorize(Policy = SkolioPolicies.PlatformAdministration)]
    public async Task<ActionResult> DeleteHousekeepingPolicy(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.HousekeepingPolicies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        dbContext.HousekeepingPolicies.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await WriteAudit("administration.housekeeping-policy.deleted", new { id, entity.PolicyName }, cancellationToken);
        return NoContent();
    }

    [HttpGet("operational-summary")]
    public async Task<ActionResult<OperationalSummaryResponse>> OperationalSummary(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        if (SchoolScope.IsPlatformAdministrator(User))
        {
            var recentAuditCount = await dbContext.AuditLogEntries.CountAsync(x => x.CreatedAtUtc >= now.AddDays(-7), cancellationToken);
            var enabledFeatureToggles = await dbContext.FeatureToggles.CountAsync(x => x.IsEnabled, cancellationToken);
            var activeLifecyclePolicies = await dbContext.SchoolYearLifecyclePolicies.CountAsync(x => x.Status == Skolio.Administration.Domain.Enums.PolicyStatus.Active, cancellationToken);
            var activeHousekeepingPolicies = await dbContext.HousekeepingPolicies.CountAsync(x => x.Status == Skolio.Administration.Domain.Enums.PolicyStatus.Active, cancellationToken);
            var latestAudit = await dbContext.AuditLogEntries.OrderByDescending(x => x.CreatedAtUtc).Take(5).Select(x => x.ActionCode).ToListAsync(cancellationToken);
            return Ok(new OperationalSummaryResponse(recentAuditCount, enabledFeatureToggles, activeLifecyclePolicies, activeHousekeepingPolicies, latestAudit));
        }

        var scopedSchoolIds = SchoolScope.GetScopedSchoolIds(User).Select(x => x.ToString()).ToList();
        var scopedAudit = await dbContext.AuditLogEntries
            .Where(x => x.CreatedAtUtc >= now.AddDays(-7))
            .Where(x => scopedSchoolIds.Any(id => x.Payload.Contains(id)))
            .ToListAsync(cancellationToken);

        var schoolScopedLifecycle = await dbContext.SchoolYearLifecyclePolicies.CountAsync(x => scopedSchoolIds.Contains(x.SchoolId.ToString()) && x.Status == Skolio.Administration.Domain.Enums.PolicyStatus.Active, cancellationToken);
        var enabledSchoolToggles = await dbContext.FeatureToggles.CountAsync(x => x.IsEnabled && EF.Functions.ILike(x.FeatureCode, "school.%"), cancellationToken);
        var delegatedHousekeeping = await dbContext.HousekeepingPolicies.CountAsync(x => x.Status == Skolio.Administration.Domain.Enums.PolicyStatus.Active && EF.Functions.ILike(x.PolicyName, "school.%"), cancellationToken);
        var latestScopedAudit = scopedAudit.OrderByDescending(x => x.CreatedAtUtc).Take(5).Select(x => x.ActionCode).ToList();

        return Ok(new OperationalSummaryResponse(scopedAudit.Count, enabledSchoolToggles, schoolScopedLifecycle, delegatedHousekeeping, latestScopedAudit));
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

    public sealed record CreateSettingRequest(string Key, string Value, bool IsSensitive);
    public sealed record UpdateSettingRequest(string Value, bool IsSensitive);
    public sealed record CreateToggleRequest(string FeatureCode, bool IsEnabled);
    public sealed record UpdateToggleRequest(bool IsEnabled);
    public sealed record CreateSchoolYearPolicyRequest(Guid SchoolId, string PolicyName, int ClosureGraceDays, bool Activate);
    public sealed record UpdateSchoolYearPolicyRequest(int ClosureGraceDays, string Status);
    public sealed record CreateHousekeepingPolicyRequest(string PolicyName, int RetentionDays, bool Activate);
    public sealed record UpdateHousekeepingPolicyRequest(int RetentionDays, string Status);
    public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int PageNumber, int PageSize, int TotalCount);
    public sealed record OperationalSummaryResponse(int RecentAuditCount, int EnabledFeatureToggles, int ActiveLifecyclePolicies, int ActiveHousekeepingPolicies, IReadOnlyCollection<string> LatestAuditActions);
}
