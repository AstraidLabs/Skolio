using System.ComponentModel.DataAnnotations;

namespace Skolio.Identity.Infrastructure.Configuration;

public sealed class JwksOptions
{
    public const string SectionName = "Identity:Jwks";

    [Required]
    public string DiscoveryPath { get; init; } = "/.well-known/jwks.json";

    [Required]
    public string KeySetId { get; init; } = "skolio-identity-signing-k1";
}
