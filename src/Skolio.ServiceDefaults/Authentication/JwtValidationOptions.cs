using System.ComponentModel.DataAnnotations;

namespace Skolio.ServiceDefaults.Authentication;

public sealed class JwtValidationOptions
{
    [Required]
    public string Authority { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = "skolio_api";

    public bool RequireHttpsMetadata { get; init; }
}
