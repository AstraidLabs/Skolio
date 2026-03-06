using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Skolio.Identity.Infrastructure.Persistence;
using Skolio.Identity.Infrastructure.Seeding;

namespace Skolio.Identity.Infrastructure.Extensions;

public static class MigrationExtensions
{
    public static async Task ApplyIdentityMigrationsAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await dbContext.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<IdentityAuthSeeder>();
        await seeder.SeedAsync(CancellationToken.None);
    }
}
