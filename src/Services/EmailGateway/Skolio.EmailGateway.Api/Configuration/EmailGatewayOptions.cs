using System.ComponentModel.DataAnnotations;

namespace Skolio.EmailGateway.Api.Configuration;

public sealed class EmailGatewayOptions
{
    public const string SectionName = "EmailGateway";

    [Required]
    public string ServiceName { get; init; } = "Skolio.EmailGateway.Api";

    [Required]
    public string InternalApiKey { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string FromAddress { get; init; } = string.Empty;

    [Required]
    public string FromDisplayName { get; init; } = string.Empty;

    [Required]
    [MinLength(1)]
    public string[] AllowedTemplateTypes { get; init; } = [];

    [Required]
    public SmtpOptions Smtp { get; init; } = new();
}

public sealed class SmtpOptions
{
    [Required]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; } = 25;

    public bool UseSsl { get; init; }

    public bool RequireAuthentication { get; init; }

    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    [Range(1, 120)]
    public int ConnectionTimeoutSeconds { get; init; } = 10;

    [Range(1, 120)]
    public int CommandTimeoutSeconds { get; init; } = 15;
}
