using System.ComponentModel.DataAnnotations;

namespace Skolio.Identity.Infrastructure.Configuration;

public sealed class OpenIddictOptions
{
    public const string SectionName = "Identity:OpenIddict";

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string[] Audiences { get; init; } = [];

    [Required]
    public OpenIddictClientSeedOptions FrontendClient { get; init; } = new();

    public OpenIddictServiceClientSeedOptions[] ServiceClients { get; init; } = [];
}
