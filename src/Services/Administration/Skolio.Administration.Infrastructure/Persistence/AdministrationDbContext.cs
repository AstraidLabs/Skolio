using Microsoft.EntityFrameworkCore;

namespace Skolio.Administration.Infrastructure.Persistence;

public sealed class AdministrationDbContext : DbContext
{
    public AdministrationDbContext(DbContextOptions<AdministrationDbContext> options)
        : base(options)
    {
    }
}
