using System.ComponentModel.DataAnnotations;

namespace Skolio.Organization.Infrastructure.Configuration;

public sealed class OrganizationRedisOptions
{
    public const string SectionName = "Organization:Redis";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;

    [Required]
    public string InstanceName { get; init; } = "skolio:organization";
}
