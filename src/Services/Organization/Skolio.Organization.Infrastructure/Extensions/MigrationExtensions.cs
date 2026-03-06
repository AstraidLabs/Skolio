using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Skolio.Organization.Infrastructure.Persistence;

namespace Skolio.Organization.Infrastructure.Extensions;

public static class MigrationExtensions
{
    public static async Task ApplyOrganizationMigrationsAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
