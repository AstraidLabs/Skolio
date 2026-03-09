using Skolio.Organization.Domain.Exceptions;

namespace Skolio.Organization.Domain.ValueObjects;

public sealed class Address
{
    private Address()
    {
    }

    private Address(string street, string city, string postalCode, string country)
    {
        Street = NormalizeRequired(street, "Street");
        City = NormalizeRequired(city, "City");
        PostalCode = NormalizeRequired(postalCode, "Postal code");
        Country = NormalizeRequired(country, "Country");
    }

    public string Street { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;

    public static Address Create(string street, string city, string postalCode, string country)
        => new(street, city, postalCode, country);

    public void Update(string street, string city, string postalCode, string country)
    {
        Street = NormalizeRequired(street, "Street");
        City = NormalizeRequired(city, "City");
        PostalCode = NormalizeRequired(postalCode, "Postal code");
        Country = NormalizeRequired(country, "Country");
    }

    private static string NormalizeRequired(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new OrganizationDomainException($"{field} is required.");
        }

        return value.Trim();
    }
}
