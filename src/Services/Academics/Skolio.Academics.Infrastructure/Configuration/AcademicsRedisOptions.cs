using System.ComponentModel.DataAnnotations;

namespace Skolio.Academics.Infrastructure.Configuration;

public sealed class AcademicsRedisOptions
{
    public const string SectionName = "Academics:Redis";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;

    [Required]
    public string InstanceName { get; init; } = "skolio:academics";
}
