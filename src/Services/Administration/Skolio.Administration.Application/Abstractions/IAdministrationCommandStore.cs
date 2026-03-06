using Skolio.Administration.Domain.Entities;

namespace Skolio.Administration.Application.Abstractions;

public interface IAdministrationCommandStore
{
    Task UpsertSystemSettingAsync(SystemSetting systemSetting, CancellationToken cancellationToken);
    Task UpsertFeatureToggleAsync(FeatureToggle featureToggle, CancellationToken cancellationToken);
    Task AddAuditLogEntryAsync(AuditLogEntry auditLogEntry, CancellationToken cancellationToken);
    Task UpsertSchoolYearLifecyclePolicyAsync(SchoolYearLifecyclePolicy policy, CancellationToken cancellationToken);
    Task UpsertHousekeepingPolicyAsync(HousekeepingPolicy policy, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
