using System.ComponentModel.DataAnnotations;

namespace Skolio.WebHost.Configuration;

public sealed class ServiceEndpointsOptions
{
    public const string SectionName = "ServiceEndpoints";

    [Required]
    public string IdentityApi { get; init; } = string.Empty;

    [Required]
    public string OrganizationApi { get; init; } = string.Empty;

    [Required]
    public string AcademicsApi { get; init; } = string.Empty;

    [Required]
    public string CommunicationApi { get; init; } = string.Empty;

    [Required]
    public string AdministrationApi { get; init; } = string.Empty;
}
