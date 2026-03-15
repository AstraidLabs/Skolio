using System.ComponentModel.DataAnnotations;

namespace Skolio.ServiceDefaults.Authentication.ClientCredentials;

public sealed class ServiceClientOptions
{
    [Required]
    [Url]
    public string Authority { get; init; } = string.Empty;

    [Required]
    public string ClientId { get; init; } = string.Empty;

    [Required]
    public string ClientSecret { get; init; } = string.Empty;

    public string Scope { get; init; } = "skolio_service_api";

    [Range(1, 120)]
    public int TokenRequestTimeoutSeconds { get; init; } = 10;
}
