using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Skolio.Academics.Infrastructure.Persistence;
using Skolio.Academics.Infrastructure.Seeding;

namespace Skolio.Academics.Infrastructure.Extensions;

public static class MigrationExtensions
{
    public static async Task ApplyAcademicsMigrationsAsync(this IHost host)
    {
        const string serviceName = "Skolio.Academics.Api";
        using var scope = host.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(serviceName);

        try
        {
            logger.LogInformation("Starting database migration for {ServiceName}.", serviceName);
            var dbContext = scope.ServiceProvider.GetRequiredService<AcademicsDbContext>();
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migration completed for {ServiceName}.", serviceName);

            var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var allowLocalSeedOutsideDevelopment = configuration.GetValue("Academics:Seed:EnableLocalMode", false);

            if (environment.IsDevelopment() || allowLocalSeedOutsideDevelopment)
            {
                var seeder = scope.ServiceProvider.GetRequiredService<AcademicsReferenceSeeder>();
                await seeder.SeedAsync(CancellationToken.None);
                logger.LogInformation("Academics seed completed for {ServiceName}.", serviceName);
            }
            else
            {
                logger.LogInformation("Academics seed skipped for {ServiceName}: environment={Environment}.", serviceName, environment.EnvironmentName);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Database migration failed for {ServiceName}.", serviceName);
            throw new InvalidOperationException($"Startup aborted because database migration failed for {serviceName}.", exception);
        }
    }
}
