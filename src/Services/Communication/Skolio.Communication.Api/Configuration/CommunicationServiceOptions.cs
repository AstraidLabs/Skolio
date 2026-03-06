using System.ComponentModel.DataAnnotations;

namespace Skolio.Communication.Api.Configuration;

public sealed class CommunicationServiceOptions
{
    public const string SectionName = "Communication:Service";

    [Required]
    public string ServiceName { get; init; } = "Skolio.Communication.Api";

    [Required]
    public string PublicBaseUrl { get; init; } = string.Empty;
}
