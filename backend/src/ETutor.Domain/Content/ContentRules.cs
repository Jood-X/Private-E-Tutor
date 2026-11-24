namespace ETutor.Domain.Content;

/// <summary>Pure rules for teacher content. No I/O; unit-tested.</summary>
public static class ContentRules
{
    /// <summary>Gap between sort orders, so most inserts don't need to renumber siblings.</summary>
    public const int SortOrderStep = 1000;

    public static int NextSortOrder(int? currentMax) => (currentMax ?? 0) + SortOrderStep;

    /// <summary>Why a course status change is refused, or null when it is allowed.</summary>
    public enum CourseStatusProblem
    {
        NoPublishedLesson,
        HasStudents,
        NotAllowed,
    }

    /// <summary>
    /// Course lifecycle: publish needs at least one published lesson (no empty courses in discovery);
    /// a course with students can't go back to Draft (archive it instead, so their history stays).
    /// </summary>
    public static CourseStatusProblem? CheckCourseStatusChange(
        ContentStatus from, ContentStatus to, bool hasPublishedLesson, bool hasActiveEnrollments) =>
        (from, to) switch
        {
            _ when from == to => null,
            (_, ContentStatus.Published) when !hasPublishedLesson => CourseStatusProblem.NoPublishedLesson,
            (ContentStatus.Draft or ContentStatus.Archived, ContentStatus.Published) => null,
            (ContentStatus.Published, ContentStatus.Archived) => null,
            (_, ContentStatus.Draft) when hasActiveEnrollments => CourseStatusProblem.HasStudents,
            (ContentStatus.Published or ContentStatus.Archived, ContentStatus.Draft) => null,
            _ => CourseStatusProblem.NotAllowed,
        };

    /// <summary>Only draft units/lessons can be deleted; published ones are archived to keep student history.</summary>
    public static bool CanDelete(ContentStatus status) => status == ContentStatus.Draft;

    /// <summary>Links must be absolute HTTPS URLs (no javascript:, file:, or plain HTTP).</summary>
    public static bool IsAllowedLink(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && !string.IsNullOrEmpty(uri.Host);
}
