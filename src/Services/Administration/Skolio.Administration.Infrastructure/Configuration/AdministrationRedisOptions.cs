using System.ComponentModel.DataAnnotations;

namespace Skolio.Administration.Infrastructure.Configuration;

public sealed class AdministrationRedisOptions
{
    public const string SectionName = "Administration:Redis";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;

    [Required]
    public string InstanceName { get; init; } = "skolio:administration";
}
