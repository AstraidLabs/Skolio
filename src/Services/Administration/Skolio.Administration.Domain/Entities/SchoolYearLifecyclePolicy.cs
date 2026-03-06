using Skolio.Administration.Domain.Enums;
using Skolio.Administration.Domain.Exceptions;

namespace Skolio.Administration.Domain.Entities;

public sealed class SchoolYearLifecyclePolicy
{
    private SchoolYearLifecyclePolicy(Guid id, Guid schoolId, string policyName, int closureGraceDays, PolicyStatus status)
    {
        Id = id;
        SchoolId = schoolId;
        PolicyName = policyName.Trim();
        ClosureGraceDays = closureGraceDays;
        Status = status;
    }

    public Guid Id { get; }
    public Guid SchoolId { get; }
    public string PolicyName { get; }
    public int ClosureGraceDays { get; }
    public PolicyStatus Status { get; private set; }

    public static SchoolYearLifecyclePolicy Create(Guid id, Guid schoolId, string policyName, int closureGraceDays)
    {
        if (id == Guid.Empty || schoolId == Guid.Empty)
            throw new AdministrationDomainException("School year lifecycle policy ids are required.");
        if (closureGraceDays < 0)
            throw new AdministrationDomainException("Closure grace days must not be negative.");

        return new SchoolYearLifecyclePolicy(id, schoolId, policyName, closureGraceDays, PolicyStatus.Draft);
    }

    public void Activate() => Status = PolicyStatus.Active;
}
