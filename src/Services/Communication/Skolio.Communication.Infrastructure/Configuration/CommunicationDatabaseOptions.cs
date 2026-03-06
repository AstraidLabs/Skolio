using System.ComponentModel.DataAnnotations;

namespace Skolio.Communication.Infrastructure.Configuration;

public sealed class CommunicationDatabaseOptions
{
    public const string SectionName = "Communication:Database";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;
}
