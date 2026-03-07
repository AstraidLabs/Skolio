using System.ComponentModel.DataAnnotations;

namespace Skolio.WebHost.Configuration;

public sealed class FrontendHostOptions
{
    public const string SectionName = "Frontend";

    [Required]
    public string ViteDevServer { get; init; } = "http://localhost:5173";

    public bool UseViteDevServer { get; init; } = true;

    [Required]
    public string SpaAssetsPath { get; init; } = "wwwroot/app";
}
