using ETutor.Application.Abstractions;
using ETutor.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace ETutor.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IAppDbContext
{
    public DbSet<Country> Countries => Set<Country>();

    public DbSet<Curriculum> Curricula => Set<Curriculum>();

    public DbSet<CurriculumAvailability> CurriculumAvailabilities => Set<CurriculumAvailability>();

    public DbSet<CurriculumStream> CurriculumStreams => Set<CurriculumStream>();

    public DbSet<GradeLevel> GradeLevels => Set<GradeLevel>();

    public DbSet<Subject> Subjects => Set<Subject>();

    public DbSet<GradeSubject> GradeSubjects => Set<GradeSubject>();

    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // RESTRICT is the default delete behavior; aggregates opt into CASCADE explicitly
        // in their configurations (docs/database-design.md §1).
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()))
        {
            if (foreignKey.DeleteBehavior == DeleteBehavior.Cascade && !foreignKey.IsOwnership)
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}
