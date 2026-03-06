using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Skolio.Academics.Infrastructure.Persistence;

namespace Skolio.Academics.Infrastructure.Extensions;

public static class MigrationExtensions
{
    public static async Task ApplyAcademicsMigrationsAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AcademicsDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
