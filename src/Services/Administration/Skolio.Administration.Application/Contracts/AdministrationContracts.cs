using Skolio.Administration.Domain.Enums;

namespace Skolio.Administration.Application.Contracts;

public sealed record SystemSettingContract(Guid Id, string Key, string Value, bool IsSensitive);
public sealed record FeatureToggleContract(Guid Id, string FeatureCode, bool IsEnabled);
public sealed record AuditLogEntryContract(Guid Id, Guid ActorUserId, string ActionCode, string Payload, DateTimeOffset CreatedAtUtc);
public sealed record SchoolYearLifecyclePolicyContract(Guid Id, Guid SchoolId, string PolicyName, int ClosureGraceDays, PolicyStatus Status);
public sealed record HousekeepingPolicyContract(Guid Id, string PolicyName, int RetentionDays, PolicyStatus Status);
