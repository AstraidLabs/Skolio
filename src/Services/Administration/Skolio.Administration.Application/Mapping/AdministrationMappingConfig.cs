using Mapster;
using Skolio.Administration.Application.Contracts;
using Skolio.Administration.Domain.Entities;

namespace Skolio.Administration.Application.Mapping;

public sealed class AdministrationMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<SystemSetting, SystemSettingContract>();
        config.NewConfig<FeatureToggle, FeatureToggleContract>();
        config.NewConfig<AuditLogEntry, AuditLogEntryContract>();
        config.NewConfig<SchoolYearLifecyclePolicy, SchoolYearLifecyclePolicyContract>();
        config.NewConfig<HousekeepingPolicy, HousekeepingPolicyContract>();
    }
}
