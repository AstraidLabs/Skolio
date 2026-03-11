using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Skolio.Academics.Infrastructure.Seeding;

public sealed class AcademicsReferenceSeeder(IConfiguration configuration, ILogger<AcademicsReferenceSeeder> logger)
{
    public Task SeedAsync(CancellationToken cancellationToken)
    {
        var seedEnabled = configuration.GetValue("Academics:Seed:Enabled", true);
        if (!seedEnabled)
        {
            logger.LogInformation("Academics reference seed is disabled by configuration.");
            return Task.CompletedTask;
        }

        logger.LogInformation("Academics reference seed completed without data mutations. Mandatory structural baseline is seeded in Organization service (school years, classes, groups, subjects) to keep identity-account boundary intact.");
        return Task.CompletedTask;
    }
}
