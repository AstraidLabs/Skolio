using System.ComponentModel.DataAnnotations;

namespace Skolio.Identity.Infrastructure.Configuration;

public sealed class OpenIddictClientSeedOptions
{
    [Required]
    public string ClientId { get; init; } = "skolio-frontend";

    [Required]
    public string DisplayName { get; init; } = "Skolio Frontend";

    [Required]
    public string[] RedirectUris { get; init; } = [];

    [Required]
    public string[] PostLogoutRedirectUris { get; init; } = [];
}
