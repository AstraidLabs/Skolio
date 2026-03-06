using System.ComponentModel.DataAnnotations;

namespace Skolio.Identity.Infrastructure.Configuration;

public sealed class IdentityRedisOptions
{
    public const string SectionName = "Identity:Redis";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;

    [Required]
    public string InstanceName { get; init; } = "skolio:identity";
}
