using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Skolio.Administration.Domain.Entities;
using Skolio.Administration.Infrastructure.Persistence;

namespace Skolio.Administration.Infrastructure.Seeding;

public sealed class AdministrationSystemSeeder(
    AdministrationDbContext dbContext,
    IConfiguration configuration,
    ILogger<AdministrationSystemSeeder> logger)
{
    private static readonly Guid SystemActorUserId = Guid.Parse("90000000-0000-0000-0000-000000000001");

    private static readonly SettingDefinition[] RequiredSettings =
    [
        new(Guid.Parse("91000000-0000-0000-0000-000000000101"), "platform.default-language", "cs-CZ", false),
        new(Guid.Parse("91000000-0000-0000-0000-000000000102"), "platform.timezone", "Europe/Prague", false),
        new(Guid.Parse("91000000-0000-0000-0000-000000000103"), "identity.bootstrap-enabled", "false", false)
    ];

    private static readonly ToggleDefinition[] RequiredFeatureToggles =
    [
        new(Guid.Parse("92000000-0000-0000-0000-000000000101"), "identity.bootstrap.platform-admin", true),
        new(Guid.Parse("92000000-0000-0000-0000-000000000102"), "identity.user-management.create-user", true),
        new(Guid.Parse("92000000-0000-0000-0000-000000000103"), "academics.daily-report", true)
    ];

    private static readonly PolicyDefinition[] RequiredHousekeepingPolicies =
    [
        new(Guid.Parse("93000000-0000-0000-0000-000000000101"), "Operations log retention", 365)
    ];

    private static readonly SchoolLifecyclePolicyDefinition[] RequiredSchoolLifecyclePolicies =
    [
        new(Guid.Parse("94000000-0000-0000-0000-000000000101"), Guid.Parse("11111111-1111-1111-1111-111111111111"), "Kindergarten lifecycle policy", 21),
        new(Guid.Parse("94000000-0000-0000-0000-000000000102"), Guid.Parse("22222222-2222-2222-2222-222222222222"), "Elementary lifecycle policy", 21),
        new(Guid.Parse("94000000-0000-0000-0000-000000000103"), Guid.Parse("33333333-3333-3333-3333-333333333333"), "Secondary lifecycle policy", 21)
    ];

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var seedEnabled = configuration.GetValue("Administration:Seed:Enabled", true);
        if (!seedEnabled)
        {
            logger.LogInformation("Administration seed is disabled by configuration.");
            return;
        }

        logger.LogInformation("Administration system seed started.");
        await EnsureSettingsAsync(cancellationToken);
        await EnsureFeatureTogglesAsync(cancellationToken);
        await EnsureHousekeepingPoliciesAsync(cancellationToken);
        await EnsureSchoolLifecyclePoliciesAsync(cancellationToken);
        await EnsureAuditBaselineAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Administration system seed completed.");
    }

    private async Task EnsureSettingsAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in RequiredSettings)
        {
            var entity = await dbContext.SystemSettings.FirstOrDefaultAsync(x => x.Key == definition.Key, cancellationToken);
            if (entity is null)
            {
                dbContext.SystemSettings.Add(SystemSetting.Create(definition.Id, definition.Key, definition.Value, definition.IsSensitive));
                continue;
            }

            if (entity.Id != definition.Id)
            {
                throw new InvalidOperationException($"Administration seed inconsistency: setting '{definition.Key}' has unexpected id.");
            }
        }
    }

    private async Task EnsureFeatureTogglesAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in RequiredFeatureToggles)
        {
            var entity = await dbContext.FeatureToggles.FirstOrDefaultAsync(x => x.FeatureCode == definition.FeatureCode, cancellationToken);
            if (entity is null)
            {
                dbContext.FeatureToggles.Add(FeatureToggle.Create(definition.Id, definition.FeatureCode, definition.IsEnabled));
                continue;
            }

            if (entity.Id != definition.Id)
            {
                throw new InvalidOperationException($"Administration seed inconsistency: feature toggle '{definition.FeatureCode}' has unexpected id.");
            }
        }
    }

    private async Task EnsureHousekeepingPoliciesAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in RequiredHousekeepingPolicies)
        {
            var entity = await dbContext.HousekeepingPolicies.FirstOrDefaultAsync(x => x.Id == definition.Id, cancellationToken);
            if (entity is null)
            {
                var policy = HousekeepingPolicy.Create(definition.Id, definition.PolicyName, definition.RetentionDays);
                policy.Activate();
                dbContext.HousekeepingPolicies.Add(policy);
                continue;
            }

            if (!string.Equals(entity.PolicyName, definition.PolicyName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Administration seed inconsistency: housekeeping policy '{definition.Id}' has unexpected name.");
            }
        }
    }

    private async Task EnsureSchoolLifecyclePoliciesAsync(CancellationToken cancellationToken)
    {
        foreach (var definition in RequiredSchoolLifecyclePolicies)
        {
            var entity = await dbContext.SchoolYearLifecyclePolicies.FirstOrDefaultAsync(x => x.Id == definition.Id, cancellationToken);
            if (entity is null)
            {
                var policy = SchoolYearLifecyclePolicy.Create(definition.Id, definition.SchoolId, definition.PolicyName, definition.ClosureGraceDays);
                policy.Activate();
                dbContext.SchoolYearLifecyclePolicies.Add(policy);
                continue;
            }

            if (entity.SchoolId != definition.SchoolId)
            {
                throw new InvalidOperationException($"Administration seed inconsistency: school lifecycle policy '{definition.Id}' has unexpected school scope.");
            }
        }
    }

    private async Task EnsureAuditBaselineAsync(CancellationToken cancellationToken)
    {
        const string actionCode = "SYSTEM_SEED_BASELINE_APPLIED";
        var exists = await dbContext.AuditLogEntries.AnyAsync(x => x.ActionCode == actionCode, cancellationToken);
        if (exists)
        {
            return;
        }

        dbContext.AuditLogEntries.Add(AuditLogEntry.Create(
            Guid.Parse("95000000-0000-0000-0000-000000000101"),
            SystemActorUserId,
            actionCode,
            "Administration seed baseline initialized.",
            DateTimeOffset.UtcNow));
    }

    private sealed record SettingDefinition(Guid Id, string Key, string Value, bool IsSensitive);
    private sealed record ToggleDefinition(Guid Id, string FeatureCode, bool IsEnabled);
    private sealed record PolicyDefinition(Guid Id, string PolicyName, int RetentionDays);
    private sealed record SchoolLifecyclePolicyDefinition(Guid Id, Guid SchoolId, string PolicyName, int ClosureGraceDays);
}
