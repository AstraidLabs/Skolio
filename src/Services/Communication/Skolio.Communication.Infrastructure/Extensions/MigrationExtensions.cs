using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Skolio.Communication.Infrastructure.Persistence;

namespace Skolio.Communication.Infrastructure.Extensions;

public static class MigrationExtensions
{
    public static async Task ApplyCommunicationMigrationsAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CommunicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
