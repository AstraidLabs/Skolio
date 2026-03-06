using Skolio.Administration.Domain.Exceptions;

namespace Skolio.Administration.Domain.Entities;

public sealed class FeatureToggle
{
    private FeatureToggle(Guid id, string featureCode, bool isEnabled)
    {
        Id = id;
        FeatureCode = featureCode.Trim();
        IsEnabled = isEnabled;
    }

    public Guid Id { get; }
    public string FeatureCode { get; }
    public bool IsEnabled { get; private set; }

    public static FeatureToggle Create(Guid id, string featureCode, bool isEnabled)
    {
        if (id == Guid.Empty)
            throw new AdministrationDomainException("Feature toggle id is required.");
        if (string.IsNullOrWhiteSpace(featureCode))
            throw new AdministrationDomainException("Feature code is required.");

        return new FeatureToggle(id, featureCode, isEnabled);
    }

    public void SetState(bool isEnabled) => IsEnabled = isEnabled;
}
