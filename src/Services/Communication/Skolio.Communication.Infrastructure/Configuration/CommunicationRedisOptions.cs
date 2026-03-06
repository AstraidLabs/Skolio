using System.ComponentModel.DataAnnotations;

namespace Skolio.Communication.Infrastructure.Configuration;

public sealed class CommunicationRedisOptions
{
    public const string SectionName = "Communication:Redis";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;

    [Required]
    public string InstanceName { get; init; } = "skolio:communication";
}
