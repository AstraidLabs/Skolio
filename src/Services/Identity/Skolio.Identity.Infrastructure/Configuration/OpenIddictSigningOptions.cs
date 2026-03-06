using System.ComponentModel.DataAnnotations;

namespace Skolio.Identity.Infrastructure.Configuration;

public sealed class OpenIddictSigningOptions
{
    public const string SectionName = "Identity:Signing";

    public bool UseDevelopmentCertificate { get; init; } = true;

    public string? CertificatePath { get; init; }

    public string? CertificatePassword { get; init; }

    [Required]
    public string KeyId { get; init; } = "skolio-identity-signing-k1";
}
