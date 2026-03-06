using System.ComponentModel.DataAnnotations;

namespace Skolio.Administration.Api.Configuration;

public sealed class JwtValidationOptions
{
    public const string SectionName = "Administration:Auth";

    [Required]
    public string Authority { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = "skolio_api";

    public bool RequireHttpsMetadata { get; init; }
}
