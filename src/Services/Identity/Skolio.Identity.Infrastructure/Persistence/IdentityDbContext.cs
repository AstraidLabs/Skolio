using Microsoft.EntityFrameworkCore;
using Skolio.Identity.Domain.Entities;

namespace Skolio.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<SchoolRoleAssignment> SchoolRoleAssignments => Set<SchoolRoleAssignment>();
    public DbSet<ParentStudentLink> ParentStudentLinks => Set<ParentStudentLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
}
