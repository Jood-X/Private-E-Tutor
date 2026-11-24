using ETutor.Domain.Catalog;
using ETutor.Domain.Common;
using ETutor.Domain.Users;

namespace ETutor.Domain.Content;

/// <summary>Lifecycle of teacher content. Students only ever see Published items inside Published parents.</summary>
public enum ContentStatus : byte
{
    Draft = 0,
    Published = 1,
    Archived = 2,
}

/// <summary>
/// A teacher's offering of one catalog subject for one grade in one academic year. This is the ownership
/// boundary: units, lessons, resources and enrollments all hang off a course owned by exactly one teacher.
/// </summary>
public sealed class Course : IAuditable
{
    public Guid Id { get; set; }

    public Guid TeacherId { get; set; }

    public int CurriculumId { get; set; }

    public int GradeLevelId { get; set; }

    public int SubjectId { get; set; }

    public int AcademicYearId { get; set; }

    public required string Title { get; set; }

    public string Description { get; set; } = string.Empty;

    /// <summary>Language the content is taught in (en/ar); independent of each viewer's UI language.</summary>
    public required string ContentLanguage { get; set; }

    public ContentStatus Status { get; set; } = ContentStatus.Draft;

    /// <summary>First time the course was published; null while it has never been published.</summary>
    public DateTime? PublishedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public TeacherProfile? Teacher { get; set; }

    public Curriculum? Curriculum { get; set; }

    public GradeLevel? GradeLevel { get; set; }

    public Subject? Subject { get; set; }

    public AcademicYear? AcademicYear { get; set; }

    public ICollection<Unit> Units { get; } = [];
}

public sealed class Unit : IAuditable
{
    public Guid Id { get; set; }

    public Guid CourseId { get; set; }

    public required string Title { get; set; }

    public string Description { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public ContentStatus Status { get; set; } = ContentStatus.Draft;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Course? Course { get; set; }

    public ICollection<Lesson> Lessons { get; } = [];
}

public sealed class Lesson : IAuditable
{
    public Guid Id { get; set; }

    public Guid UnitId { get; set; }

    /// <summary>Denormalized for fast access checks; kept consistent by a composite FK to (Unit.Id, Unit.CourseId).</summary>
    public Guid CourseId { get; set; }

    public required string Title { get; set; }

    public string Summary { get; set; } = string.Empty;

    /// <summary>Written lesson notes (plain text / Markdown).</summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>Teacher's estimate; null when not provided.</summary>
    public short? EstimatedMinutes { get; set; }

    public int SortOrder { get; set; }

    public ContentStatus Status { get; set; } = ContentStatus.Draft;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Unit? Unit { get; set; }

    public ICollection<LessonResource> Resources { get; } = [];
}

/// <summary>Kinds of lesson material. Video (Phase 4) reuses this table with its own column.</summary>
public enum ResourceType : byte
{
    Document = 1,
    Link = 2,
}

/// <summary>
/// One piece of lesson material. Exactly the column matching <see cref="Type"/> is set (CHECK constraint):
/// Document → <see cref="FileId"/>, Link → <see cref="ExternalUrl"/>.
/// </summary>
public sealed class LessonResource : IAuditable
{
    public Guid Id { get; set; }

    public Guid LessonId { get; set; }

    public ResourceType Type { get; set; }

    public required string Title { get; set; }

    public int SortOrder { get; set; }

    public bool IsRequired { get; set; } = true;

    public Guid? FileId { get; set; }

    public string? ExternalUrl { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Lesson? Lesson { get; set; }

    public Files.StoredFile? File { get; set; }
}
