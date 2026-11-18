using ETutor.Application.Abstractions;
using ETutor.Application.Common.Results;
using ETutor.Domain.Catalog;
using ETutor.Domain.Users;
using ETutor.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ETutor.Infrastructure.Persistence;

/// <summary>
/// The single database context. It also hosts the ASP.NET Core Identity tables, so creating an account
/// and its profile can share one transaction.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IAppDbContext
{
    private const string DomainNamespacePrefix = "ETutor.Domain";

    public DbSet<Country> Countries => Set<Country>();

    public DbSet<Curriculum> Curricula => Set<Curriculum>();

    public DbSet<CurriculumAvailability> CurriculumAvailabilities => Set<CurriculumAvailability>();

    public DbSet<CurriculumStream> CurriculumStreams => Set<CurriculumStream>();

    public DbSet<GradeLevel> GradeLevels => Set<GradeLevel>();

    public DbSet<Subject> Subjects => Set<Subject>();

    public DbSet<GradeSubject> GradeSubjects => Set<GradeSubject>();

    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<TeacherProfile> TeacherProfiles => Set<TeacherProfile>();

    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();

    public DbSet<StudentSubject> StudentSubjects => Set<StudentSubject>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public async Task<Result<T>> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // With retry-on-failure enabled, a user transaction must run inside the execution strategy so
        // the whole unit is retried, not a single statement.
        var strategy = Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async ct =>
        {
            ChangeTracker.Clear();
            await using var transaction = await Database.BeginTransactionAsync(ct);
            var result = await operation(ct);
            if (result.IsSuccess)
            {
                await transaction.CommitAsync(ct);
            }
            else
            {
                await transaction.RollbackAsync(ct);
                ChangeTracker.Clear();
            }

            return result;
        }, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // RESTRICT is the default delete behavior for our tables; aggregates opt into CASCADE
        // explicitly (docs/database-design.md §1). Identity's own tables keep their defaults.
        var domainForeignKeys = builder.Model.GetEntityTypes()
            .Where(entity => entity.ClrType.Namespace?.StartsWith(DomainNamespacePrefix, StringComparison.Ordinal) == true)
            .SelectMany(entity => entity.GetForeignKeys());
        foreach (var foreignKey in domainForeignKeys)
        {
            if (foreignKey.DeleteBehavior == DeleteBehavior.Cascade && !foreignKey.IsOwnership && !IsExplicitCascade(foreignKey))
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }

    private static bool IsExplicitCascade(Microsoft.EntityFrameworkCore.Metadata.IMutableForeignKey foreignKey) =>
        foreignKey.FindAnnotation(CascadeAnnotation) is not null;

    /// <summary>Marks a foreign key whose CASCADE is intentional (inside one aggregate).</summary>
    internal const string CascadeAnnotation = "ETutor:IntentionalCascade";
}
