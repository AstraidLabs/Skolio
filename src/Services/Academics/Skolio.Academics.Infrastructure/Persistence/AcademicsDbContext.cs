using Microsoft.EntityFrameworkCore;
using Skolio.Academics.Domain.Entities;

namespace Skolio.Academics.Infrastructure.Persistence;

public sealed class AcademicsDbContext : DbContext
{
    public AcademicsDbContext(DbContextOptions<AcademicsDbContext> options) : base(options) { }

    public DbSet<TimetableEntry> TimetableEntries => Set<TimetableEntry>();
    public DbSet<LessonRecord> LessonRecords => Set<LessonRecord>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<ExcuseNote> ExcuseNotes => Set<ExcuseNote>();
    public DbSet<GradeEntry> GradeEntries => Set<GradeEntry>();
    public DbSet<HomeworkAssignment> HomeworkAssignments => Set<HomeworkAssignment>();
    public DbSet<DailyReport> DailyReports => Set<DailyReport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(AcademicsDbContext).Assembly);
}
