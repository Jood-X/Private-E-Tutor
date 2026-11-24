using ETutor.Domain.Content;

namespace ETutor.Application.Courses;

// ---- Requests ----

public sealed record CreateCourseRequest(
    string Title,
    string Description,
    string ContentLanguage,
    int CurriculumId,
    int GradeLevelId,
    int SubjectId,
    int AcademicYearId);

/// <summary>Catalog placement (curriculum, grade, subject, year) is fixed at creation; create a new course to change it.</summary>
public sealed record UpdateCourseRequest(string Title, string Description, string ContentLanguage);

public sealed record ChangeStatusRequest(ContentStatus Status);

public sealed record UnitRequest(string Title, string Description);

public sealed record LessonRequest(string Title, string Summary, string Notes, short? EstimatedMinutes);

public sealed record MoveRequest(int Position);

public sealed record LinkResourceRequest(string Title, string Url, bool IsRequired);

/// <summary>An uploaded document as received by the API (the stream is owned by the caller).</summary>
public sealed record DocumentUpload(string Title, bool IsRequired, string FileName, string ContentType, long Length, Stream Content);

// ---- Responses ----

/// <summary>Row in the teacher's "My courses" list.</summary>
public sealed record TeacherCourseSummaryDto(
    Guid Id,
    string Title,
    ContentStatus Status,
    string SubjectName,
    string GradeLevelName,
    string CurriculumName,
    int UnitCount,
    int LessonCount,
    int PendingRequests,
    int ActiveStudents,
    DateTime UpdatedAtUtc);

/// <summary>Full course structure for its owner (all statuses).</summary>
public sealed record TeacherCourseDto(
    Guid Id,
    string Title,
    string Description,
    string ContentLanguage,
    ContentStatus Status,
    int CurriculumId,
    int GradeLevelId,
    int SubjectId,
    int AcademicYearId,
    string SubjectName,
    string GradeLevelName,
    IReadOnlyList<TeacherUnitDto> Units);

public sealed record TeacherUnitDto(Guid Id, string Title, string Description, ContentStatus Status, IReadOnlyList<TeacherLessonDto> Lessons);

public sealed record TeacherLessonDto(Guid Id, string Title, ContentStatus Status, short? EstimatedMinutes, int ResourceCount);

public sealed record LessonDetailDto(
    Guid Id,
    Guid UnitId,
    Guid CourseId,
    string Title,
    string Summary,
    string Notes,
    short? EstimatedMinutes,
    ContentStatus Status,
    IReadOnlyList<ResourceDto> Resources);

/// <summary>
/// A lesson resource. <c>Url</c> is set for links; documents are fetched from the download endpoint; videos carry
/// display details in <c>Video</c> and are played through the playback endpoint.
/// </summary>
public sealed record ResourceDto(
    Guid Id,
    ResourceType Type,
    string Title,
    bool IsRequired,
    string? Url,
    string? FileName,
    long? SizeBytes,
    ResourceVideoDto? Video = null);

public sealed record ResourceVideoDto(int? DurationSeconds, string? ThumbnailUrl, VideoStatus Status);
