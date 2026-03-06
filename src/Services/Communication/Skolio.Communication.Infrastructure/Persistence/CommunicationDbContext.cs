using Microsoft.EntityFrameworkCore;

namespace Skolio.Communication.Infrastructure.Persistence;

public sealed class CommunicationDbContext : DbContext
{
    public CommunicationDbContext(DbContextOptions<CommunicationDbContext> options)
        : base(options)
    {
    }
}
