using System.ComponentModel.DataAnnotations;

namespace Skolio.WebHost.Configuration;

public sealed class ServiceEndpointsOptions
{
    public const string SectionName = "ServiceEndpoints";

    [Required]
    public string IdentityApi { get; init; } = string.Empty;

    [Required]
    public string OidcClientId { get; init; } = "skolio-frontend";

    [Required]
    public string OidcRedirectUri { get; init; } = "http://localhost:8080/auth/callback";

    [Required]
    public string OidcPostLogoutRedirectUri { get; init; } = "http://localhost:8080/";

    [Required]
    public string OidcScope { get; init; } = "openid profile skolio_api";

    [Required]
    public string OrganizationApi { get; init; } = string.Empty;

    [Required]
    public string AcademicsApi { get; init; } = string.Empty;

    [Required]
    public string CommunicationApi { get; init; } = string.Empty;

    [Required]
    public string AdministrationApi { get; init; } = string.Empty;
}
