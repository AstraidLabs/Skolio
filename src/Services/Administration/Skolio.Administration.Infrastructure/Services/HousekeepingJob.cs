using Microsoft.Extensions.Logging;

namespace Skolio.Administration.Infrastructure.Services;

public sealed class HousekeepingJob(ILogger<HousekeepingJob> logger)
{
    public Task ExecuteAsync()
    {
        logger.LogInformation("Housekeeping recurring job executed.");
        return Task.CompletedTask;
    }
}
