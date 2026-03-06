using System.ComponentModel.DataAnnotations;

namespace Skolio.Identity.Infrastructure.Configuration;

public sealed class IdentityDatabaseOptions
{
    public const string SectionName = "Identity:Database";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;
}
