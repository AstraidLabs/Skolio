using Skolio.Administration.Domain.Enums;
using Skolio.Administration.Domain.Exceptions;

namespace Skolio.Administration.Domain.Entities;

public sealed class HousekeepingPolicy
{
    private HousekeepingPolicy(Guid id, string policyName, int retentionDays, PolicyStatus status)
    {
        Id = id;
        PolicyName = policyName.Trim();
        RetentionDays = retentionDays;
        Status = status;
    }

    public Guid Id { get; }
    public string PolicyName { get; }
    public int RetentionDays { get; }
    public PolicyStatus Status { get; private set; }

    public static HousekeepingPolicy Create(Guid id, string policyName, int retentionDays)
    {
        if (id == Guid.Empty)
            throw new AdministrationDomainException("Housekeeping policy id is required.");
        if (retentionDays <= 0)
            throw new AdministrationDomainException("Housekeeping retention days must be positive.");

        return new HousekeepingPolicy(id, policyName, retentionDays, PolicyStatus.Draft);
    }

    public void Activate() => Status = PolicyStatus.Active;
}
