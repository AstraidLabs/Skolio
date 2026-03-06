using System.ComponentModel.DataAnnotations;

namespace Skolio.Administration.Api.Configuration;

public sealed class AdministrationServiceOptions
{
    public const string SectionName = "Administration:Service";

    [Required]
    public string ServiceName { get; init; } = "Skolio.Administration.Api";

    [Required]
    public string PublicBaseUrl { get; init; } = string.Empty;
}
