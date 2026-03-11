using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;
using Skolio.Identity.Domain.Entities;
using Skolio.Identity.Infrastructure.Auth;

namespace Skolio.Identity.Infrastructure.Persistence;

public sealed class IdentityDbContext : IdentityDbContext<SkolioIdentityUser, SkolioIdentityRole, string>
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<SchoolRoleAssignment> SchoolRoleAssignments => Set<SchoolRoleAssignment>();
    public DbSet<ParentStudentLink> ParentStudentLinks => Set<ParentStudentLink>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.UseOpenIddict();

        modelBuilder.Entity<SkolioIdentityUser>(builder =>
        {
            builder.Property(x => x.InviteTokenHash).HasMaxLength(128);
            builder.Property(x => x.InviteCodeHash).HasMaxLength(128);
        });
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);
    }
}


