using System.ComponentModel.DataAnnotations;

namespace Skolio.Organization.Infrastructure.Configuration;

public sealed class OrganizationDatabaseOptions
{
    public const string SectionName = "Organization:Database";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;
}
