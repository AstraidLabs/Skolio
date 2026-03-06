using System.ComponentModel.DataAnnotations;

namespace Skolio.Identity.Infrastructure.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Identity:Jwt";

    [Required]
    public string AccessTokenLifetime { get; init; } = "00:30:00";

    [Required]
    public string RefreshTokenLifetime { get; init; } = "7.00:00:00";
}
