using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Skolio.Communication.Infrastructure.Persistence;

namespace Skolio.Communication.Infrastructure.Extensions;

public static class MigrationExtensions
{
    public static async Task ApplyCommunicationMigrationsAsync(this IHost host)
    {
        const string serviceName = "Skolio.Communication.Api";
        using var scope = host.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(serviceName);

        try
        {
            logger.LogInformation("Starting database migration for {ServiceName}.", serviceName);
            var dbContext = scope.ServiceProvider.GetRequiredService<CommunicationDbContext>();
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migration completed for {ServiceName}.", serviceName);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Database migration failed for {ServiceName}.", serviceName);
            throw new InvalidOperationException($"Startup aborted because database migration failed for {serviceName}.", exception);
        }
    }
}
