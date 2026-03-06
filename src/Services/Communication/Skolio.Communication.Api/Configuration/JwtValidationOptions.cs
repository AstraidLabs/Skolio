using System.ComponentModel.DataAnnotations;

namespace Skolio.Communication.Api.Configuration;

public sealed class JwtValidationOptions
{
    public const string SectionName = "Communication:Auth";

    [Required]
    public string Authority { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = "skolio_api";

    public bool RequireHttpsMetadata { get; init; }
}
