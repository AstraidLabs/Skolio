using System.ComponentModel.DataAnnotations;

namespace Skolio.Administration.Infrastructure.Configuration;

public sealed class AdministrationHangfireOptions
{
    public const string SectionName = "Administration:Hangfire";

    [Required]
    public string StorageConnectionString { get; init; } = string.Empty;

    [Required]
    public string SchemaName { get; init; } = "hangfire";
}
