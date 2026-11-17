using ETutor.Domain.Catalog;
using Microsoft.EntityFrameworkCore;

namespace ETutor.Application.Abstractions;

/// <summary>
/// The persistence port used by use-case services. It exposes <see cref="DbSet{TEntity}"/> so that
/// services can project exactly the data a use case needs (see docs/architecture.md §2.4).
/// </summary>
public interface IAppDbContext
{
    DbSet<Country> Countries { get; }

    DbSet<Curriculum> Curricula { get; }

    DbSet<CurriculumAvailability> CurriculumAvailabilities { get; }

    DbSet<CurriculumStream> CurriculumStreams { get; }

    DbSet<GradeLevel> GradeLevels { get; }

    DbSet<Subject> Subjects { get; }

    DbSet<GradeSubject> GradeSubjects { get; }

    DbSet<AcademicYear> AcademicYears { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
