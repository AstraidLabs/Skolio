using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Data;
using Skolio.Organization.Infrastructure.Persistence;
using Skolio.Organization.Infrastructure.Seeding;

namespace Skolio.Organization.Infrastructure.Extensions;

public static class MigrationExtensions
{
    public static async Task ApplyOrganizationMigrationsAsync(this IHost host)
    {
        const string serviceName = "Skolio.Organization.Api";
        using var scope = host.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(serviceName);

        try
        {
            logger.LogInformation("Starting database migration for {ServiceName}.", serviceName);
            var dbContext = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
            var hasMigrations = dbContext.Database.GetMigrations().Any();
            if (hasMigrations)
            {
                await BootstrapExistingSchemaHistoryAsync(dbContext, logger);
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Database migration completed for {ServiceName}.", serviceName);
            }
            else
            {
                await dbContext.Database.EnsureCreatedAsync();
                var schoolsTableExists = await TableExistsAsync(dbContext, "schools", CancellationToken.None);
                if (!schoolsTableExists)
                {
                    logger.LogWarning("Schema bootstrap found no 'schools' table for {ServiceName}; repairing __EFMigrationsHistory and retrying EnsureCreated.", serviceName);
                    await dbContext.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS \"__EFMigrationsHistory\";");
                    await dbContext.Database.EnsureCreatedAsync();
                }

                logger.LogInformation("Database schema ensured via EnsureCreated for {ServiceName}.", serviceName);
            }

            var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var allowLocalSeedOutsideDevelopment = configuration.GetValue("Organization:Seed:EnableLocalMode", false);

            if (environment.IsDevelopment() || allowLocalSeedOutsideDevelopment)
            {
                var seeder = scope.ServiceProvider.GetRequiredService<OrganizationDevelopmentSeeder>();
                await seeder.SeedAsync(CancellationToken.None);
                logger.LogInformation("Organization seed completed for {ServiceName}.", serviceName);
            }
            else
            {
                logger.LogInformation("Organization seed skipped for {ServiceName}: environment={Environment}.", serviceName, environment.EnvironmentName);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Database migration failed for {ServiceName}.", serviceName);
            throw new InvalidOperationException($"Startup aborted because database migration failed for {serviceName}.", exception);
        }
    }

    private static async Task BootstrapExistingSchemaHistoryAsync(OrganizationDbContext dbContext, ILogger logger)
    {
        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();
        if (appliedMigrations.Any())
        {
            return;
        }

        var schoolsTableExists = await TableExistsAsync(dbContext, "schools", CancellationToken.None);
        var schoolOperatorsTableExists = await TableExistsAsync(dbContext, "school_operators", CancellationToken.None);
        var foundersTableExists = await TableExistsAsync(dbContext, "founders", CancellationToken.None);
        if (!schoolsTableExists || !schoolOperatorsTableExists || !foundersTableExists)
        {
            return;
        }

        var firstMigration = dbContext.Database.GetMigrations().FirstOrDefault();
        if (string.IsNullOrWhiteSpace(firstMigration))
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" character varying(150) NOT NULL,
                "ProductVersion" character varying(32) NOT NULL,
                CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
            );
            """);

        await dbContext.Database.ExecuteSqlRawAsync(
            "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ({0}, {1}) ON CONFLICT (\"MigrationId\") DO NOTHING;",
            firstMigration,
            "10.0.3");

        logger.LogInformation("Existing schema detected; bootstrap migration history inserted for migration {MigrationId}.", firstMigration);
    }

    private static async Task<bool> TableExistsAsync(OrganizationDbContext dbContext, string tableName, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        try
        {
            if (shouldCloseConnection)
            {
                await connection.OpenAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = @tableName);";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@tableName";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            var scalarResult = await command.ExecuteScalarAsync(cancellationToken);
            return scalarResult is bool exists && exists;
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }
}
