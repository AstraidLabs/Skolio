using Microsoft.EntityFrameworkCore;
using Skolio.Organization.Domain.Entities;

namespace Skolio.Organization.Infrastructure.Persistence;

public sealed class OrganizationDbContext : DbContext
{
    public OrganizationDbContext(DbContextOptions<OrganizationDbContext> options)
        : base(options)
    {
    }

    public DbSet<School> Schools => Set<School>();
    public DbSet<SchoolOperator> SchoolOperators => Set<SchoolOperator>();
    public DbSet<Founder> Founders => Set<Founder>();
    public DbSet<SchoolYear> SchoolYears => Set<SchoolYear>();
    public DbSet<GradeLevel> GradeLevels => Set<GradeLevel>();
    public DbSet<ClassRoom> ClassRooms => Set<ClassRoom>();
    public DbSet<TeachingGroup> TeachingGroups => Set<TeachingGroup>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<TeacherAssignment> TeacherAssignments => Set<TeacherAssignment>();
    public DbSet<SecondaryFieldOfStudy> SecondaryFieldsOfStudy => Set<SecondaryFieldOfStudy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrganizationDbContext).Assembly);
    }
}
