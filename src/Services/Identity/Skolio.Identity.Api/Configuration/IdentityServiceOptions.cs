using System.ComponentModel.DataAnnotations;

namespace Skolio.Identity.Api.Configuration;

public sealed class IdentityServiceOptions
{
    public const string SectionName = "Identity:Service";

    [Required]
    public string ServiceName { get; init; } = "Skolio.Identity.Api";

    [Required]
    public string PublicBaseUrl { get; init; } = string.Empty;
}
