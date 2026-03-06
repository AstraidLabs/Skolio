using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Skolio.Administration.Infrastructure.Persistence;

namespace Skolio.Administration.Infrastructure.Extensions;
public static class MigrationExtensions
{
    public static async Task ApplyAdministrationMigrationsAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AdministrationDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
