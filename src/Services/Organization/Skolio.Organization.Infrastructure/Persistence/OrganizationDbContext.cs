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
    public DbSet<SchoolContextScopeMatrix> SchoolContextScopeMatrices => Set<SchoolContextScopeMatrix>();
    public DbSet<SchoolContextScopeCapability> SchoolContextScopeCapabilities => Set<SchoolContextScopeCapability>();
    public DbSet<SchoolContextScopeAllowedRole> SchoolContextScopeAllowedRoles => Set<SchoolContextScopeAllowedRole>();
    public DbSet<SchoolContextScopeAllowedProfileSection> SchoolContextScopeAllowedProfileSections => Set<SchoolContextScopeAllowedProfileSection>();
    public DbSet<SchoolContextScopeAllowedCreateUserFlow> SchoolContextScopeAllowedCreateUserFlows => Set<SchoolContextScopeAllowedCreateUserFlow>();
    public DbSet<SchoolContextScopeAllowedUserManagementFlow> SchoolContextScopeAllowedUserManagementFlows => Set<SchoolContextScopeAllowedUserManagementFlow>();
    public DbSet<SchoolContextScopeAllowedOrganizationSection> SchoolContextScopeAllowedOrganizationSections => Set<SchoolContextScopeAllowedOrganizationSection>();
    public DbSet<SchoolContextScopeAllowedAcademicsSection> SchoolContextScopeAllowedAcademicsSections => Set<SchoolContextScopeAllowedAcademicsSection>();
    public DbSet<SchoolScopeOverride> SchoolScopeOverrides => Set<SchoolScopeOverride>();
    public DbSet<SchoolPlaceOfEducation> SchoolPlacesOfEducation => Set<SchoolPlaceOfEducation>();
    public DbSet<SchoolCapacity> SchoolCapacities => Set<SchoolCapacity>();
    public DbSet<RoleDefinition> RoleDefinitions => Set<RoleDefinition>();
    public DbSet<OrganizationSchoolStructureMatrixEntry> OrganizationSchoolStructureMatrixEntries => Set<OrganizationSchoolStructureMatrixEntry>();
    public DbSet<OrganizationRegistryMatrixEntry> OrganizationRegistryMatrixEntries => Set<OrganizationRegistryMatrixEntry>();
    public DbSet<OrganizationCapacityMatrixEntry> OrganizationCapacityMatrixEntries => Set<OrganizationCapacityMatrixEntry>();
    public DbSet<OrganizationAcademicStructureMatrixEntry> OrganizationAcademicStructureMatrixEntries => Set<OrganizationAcademicStructureMatrixEntry>();
    public DbSet<OrganizationAssignmentMatrixEntry> OrganizationAssignmentMatrixEntries => Set<OrganizationAssignmentMatrixEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrganizationDbContext).Assembly);
    }
}
