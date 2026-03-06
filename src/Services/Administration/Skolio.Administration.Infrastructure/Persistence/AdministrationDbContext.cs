using Microsoft.EntityFrameworkCore;
using Skolio.Administration.Domain.Entities;

namespace Skolio.Administration.Infrastructure.Persistence;

public sealed class AdministrationDbContext : DbContext
{
    public AdministrationDbContext(DbContextOptions<AdministrationDbContext> options) : base(options) { }
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<FeatureToggle> FeatureToggles => Set<FeatureToggle>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();
    public DbSet<SchoolYearLifecyclePolicy> SchoolYearLifecyclePolicies => Set<SchoolYearLifecyclePolicy>();
    public DbSet<HousekeepingPolicy> HousekeepingPolicies => Set<HousekeepingPolicy>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdministrationDbContext).Assembly);
}
