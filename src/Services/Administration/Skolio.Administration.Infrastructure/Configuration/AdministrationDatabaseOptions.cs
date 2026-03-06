using System.ComponentModel.DataAnnotations;

namespace Skolio.Administration.Infrastructure.Configuration;

public sealed class AdministrationDatabaseOptions
{
    public const string SectionName = "Administration:Database";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;
}
