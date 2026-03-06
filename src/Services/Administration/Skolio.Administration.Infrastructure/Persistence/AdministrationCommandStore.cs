using Skolio.Administration.Application.Abstractions;
using Skolio.Administration.Domain.Entities;

namespace Skolio.Administration.Infrastructure.Persistence;
public sealed class AdministrationCommandStore(AdministrationDbContext dbContext) : IAdministrationCommandStore
{
    public Task UpsertSystemSettingAsync(SystemSetting systemSetting, CancellationToken cancellationToken) { dbContext.SystemSettings.Update(systemSetting); return Task.CompletedTask; }
    public Task UpsertFeatureToggleAsync(FeatureToggle featureToggle, CancellationToken cancellationToken) { dbContext.FeatureToggles.Update(featureToggle); return Task.CompletedTask; }
    public Task AddAuditLogEntryAsync(AuditLogEntry auditLogEntry, CancellationToken cancellationToken) => dbContext.AuditLogEntries.AddAsync(auditLogEntry, cancellationToken).AsTask();
    public Task UpsertSchoolYearLifecyclePolicyAsync(SchoolYearLifecyclePolicy policy, CancellationToken cancellationToken) { dbContext.SchoolYearLifecyclePolicies.Update(policy); return Task.CompletedTask; }
    public Task UpsertHousekeepingPolicyAsync(HousekeepingPolicy policy, CancellationToken cancellationToken) { dbContext.HousekeepingPolicies.Update(policy); return Task.CompletedTask; }
    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
