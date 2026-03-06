using Microsoft.EntityFrameworkCore;

namespace Skolio.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }
}
