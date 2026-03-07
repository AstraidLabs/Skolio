using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Skolio.Identity.Infrastructure.Persistence;
using Skolio.Identity.Infrastructure.Seeding;

namespace Skolio.Identity.Infrastructure.Extensions;

public static class MigrationExtensions
{
    public static async Task ApplyIdentityMigrationsAsync(this IHost host)
    {
        const string serviceName = "Skolio.Identity.Api";
        using var scope = host.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(serviceName);

        try
        {
            logger.LogInformation("Starting database migration for {ServiceName}.", serviceName);
            var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

            if (dbContext.Database.GetMigrations().Any())
            {
                await dbContext.Database.MigrateAsync();
            }
            else
            {
                await dbContext.Database.EnsureDeletedAsync();
                await dbContext.Database.EnsureCreatedAsync();
            }

            logger.LogInformation("Database migration completed for {ServiceName}.", serviceName);

            var seeder = scope.ServiceProvider.GetRequiredService<IdentityAuthSeeder>();
            await seeder.SeedAsync(CancellationToken.None);
            logger.LogInformation("Identity seed completed for {ServiceName}.", serviceName);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Database migration failed for {ServiceName}.", serviceName);
            throw new InvalidOperationException($"Startup aborted because database migration failed for {serviceName}.", exception);
        }
    }
}
