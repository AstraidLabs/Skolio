using System.ComponentModel.DataAnnotations;

namespace Skolio.Identity.Infrastructure.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Identity:Jwt";

    [Required]
    public string AccessTokenLifetime { get; init; } = "00:30:00";

    public bool IssueRefreshTokens { get; init; }
}
