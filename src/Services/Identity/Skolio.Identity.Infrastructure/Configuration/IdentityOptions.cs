using System.ComponentModel.DataAnnotations;

namespace Skolio.Identity.Infrastructure.Configuration;

public sealed class IdentityOptions
{
    public const string SectionName = "Identity:Identity";

    [Range(6, 256)]
    public int RequiredPasswordLength { get; init; } = 12;

    public bool RequireNonAlphanumeric { get; init; } = true;
}
