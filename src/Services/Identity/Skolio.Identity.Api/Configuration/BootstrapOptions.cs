using System.ComponentModel.DataAnnotations;

namespace Skolio.Identity.Api.Configuration;

public sealed class BootstrapOptions
{
    public const string SectionName = "Identity:Bootstrap";

    [Required]
    public bool PlatformAdminEnabled { get; init; }
}
