using Microsoft.EntityFrameworkCore;

namespace Skolio.Academics.Infrastructure.Persistence;

public sealed class AcademicsDbContext : DbContext
{
    public AcademicsDbContext(DbContextOptions<AcademicsDbContext> options)
        : base(options)
    {
    }
}
