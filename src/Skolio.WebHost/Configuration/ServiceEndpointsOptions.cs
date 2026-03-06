using System.ComponentModel.DataAnnotations;

namespace Skolio.WebHost.Configuration;

public sealed class ServiceEndpointsOptions
{
    public const string SectionName = "ServiceEndpoints";

    [Required]
    [Url]
    public string IdentityApi { get; init; } = string.Empty;

    [Required]
    public string OidcClientId { get; init; } = "skolio-frontend";

    [Required]
    [Url]
    public string OidcRedirectUri { get; init; } = "http://localhost:8080/auth/callback";

    [Required]
    [Url]
    public string OidcPostLogoutRedirectUri { get; init; } = "http://localhost:8080/";

    [Required]
    public string OidcScope { get; init; } = "openid profile skolio_api";

    [Required]
    [Url]
    public string OrganizationApi { get; init; } = string.Empty;

    [Required]
    [Url]
    public string AcademicsApi { get; init; } = string.Empty;

    [Required]
    [Url]
    public string CommunicationApi { get; init; } = string.Empty;

    [Required]
    [Url]
    public string AdministrationApi { get; init; } = string.Empty;
}
