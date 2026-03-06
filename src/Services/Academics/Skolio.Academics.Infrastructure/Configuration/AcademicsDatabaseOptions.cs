using System.ComponentModel.DataAnnotations;

namespace Skolio.Academics.Infrastructure.Configuration;

public sealed class AcademicsDatabaseOptions
{
    public const string SectionName = "Academics:Database";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;
}
