using ETutor.Application.Common.Results;
using ETutor.Domain.Catalog;
using ETutor.Domain.Content;
using ETutor.Domain.Enrollments;
using ETutor.Domain.Files;
using ETutor.Domain.Users;
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

    DbSet<UserProfile> UserProfiles { get; }

    DbSet<TeacherProfile> TeacherProfiles { get; }

    DbSet<StudentProfile> StudentProfiles { get; }

    DbSet<StudentSubject> StudentSubjects { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Course> Courses { get; }

    DbSet<Unit> Units { get; }

    DbSet<Lesson> Lessons { get; }

    DbSet<LessonResource> LessonResources { get; }

    DbSet<StoredFile> StoredFiles { get; }

    DbSet<Enrollment> Enrollments { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="operation"/> in one database transaction (retried as a whole on transient
    /// failures). The transaction commits only if the operation returns a successful result.
    /// </summary>
    Task<Result<T>> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken);
}
