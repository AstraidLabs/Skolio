using System.ComponentModel.DataAnnotations;

namespace Skolio.Organization.Api.Configuration;

public sealed class OrganizationServiceOptions
{
    public const string SectionName = "Organization:Service";

    [Required]
    public string ServiceName { get; init; } = "Skolio.Organization.Api";

    [Required]
    public string PublicBaseUrl { get; init; } = string.Empty;
}
