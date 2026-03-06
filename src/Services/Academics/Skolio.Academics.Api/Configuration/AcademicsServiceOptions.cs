using System.ComponentModel.DataAnnotations;

namespace Skolio.Academics.Api.Configuration;

public sealed class AcademicsServiceOptions
{
    public const string SectionName = "Academics:Service";

    [Required]
    public string ServiceName { get; init; } = "Skolio.Academics.Api";

    [Required]
    public string PublicBaseUrl { get; init; } = string.Empty;
}
