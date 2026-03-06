using Skolio.Administration.Domain.Exceptions;

namespace Skolio.Administration.Domain.Entities;

public sealed class SystemSetting
{
    private SystemSetting(Guid id, string key, string value, bool isSensitive)
    {
        Id = id;
        Key = key.Trim();
        Value = value.Trim();
        IsSensitive = isSensitive;
    }

    public Guid Id { get; }
    public string Key { get; }
    public string Value { get; private set; }
    public bool IsSensitive { get; }

    public static SystemSetting Create(Guid id, string key, string value, bool isSensitive)
    {
        if (id == Guid.Empty)
            throw new AdministrationDomainException("System setting id is required.");
        if (string.IsNullOrWhiteSpace(key))
            throw new AdministrationDomainException("System setting key is required.");

        return new SystemSetting(id, key, value, isSensitive);
    }

    public void UpdateValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new AdministrationDomainException("System setting value is required.");

        Value = value.Trim();
    }
}
