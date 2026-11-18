using ETutor.Domain.Catalog;

namespace ETutor.Domain.Users;

/// <summary>
/// What a student studies. The grade and stream are guaranteed by composite foreign keys to belong
/// to <see cref="CurriculumId"/> (docs/database-design.md §4).
/// </summary>
public sealed class StudentProfile
{
    public Guid UserId { get; set; }

    public int CurriculumId { get; set; }

    public int GradeLevelId { get; set; }

    /// <summary>Null when the curriculum has no streams or none was chosen.</summary>
    public int? StreamId { get; set; }

    public UserProfile? User { get; set; }

    public Curriculum? Curriculum { get; set; }

    public GradeLevel? GradeLevel { get; set; }

    public CurriculumStream? Stream { get; set; }

    public ICollection<StudentSubject> Subjects { get; } = [];
}

/// <summary>A subject the student is interested in (used for discovery).</summary>
public sealed class StudentSubject
{
    public Guid StudentId { get; set; }

    public int SubjectId { get; set; }

    public StudentProfile? Student { get; set; }

    public Subject? Subject { get; set; }
}
