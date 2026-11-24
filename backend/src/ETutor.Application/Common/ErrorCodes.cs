namespace ETutor.Application.Common;

/// <summary>
/// Stable error codes shared with clients. Never rename a published code; clients map them to
/// localized messages. Format: "area.reason".
/// </summary>
public static class ErrorCodes
{
    public static class Common
    {
        public const string ValidationFailed = "common.validation_failed";
        public const string NotFound = "common.not_found";
        public const string MethodNotAllowed = "common.method_not_allowed";
        public const string Unauthorized = "common.unauthorized";
        public const string Forbidden = "common.forbidden";
        public const string RateLimited = "common.rate_limited";
        public const string BadRequest = "common.bad_request";
        public const string Unexpected = "common.unexpected";
        public const string ConcurrencyConflict = "common.concurrency_conflict";
    }

    public static class Auth
    {
        public const string EmailTaken = "auth.email_taken";
        public const string WeakPassword = "auth.weak_password";
        public const string InvalidCredentials = "auth.invalid_credentials";
        public const string LockedOut = "auth.locked_out";
        public const string InvalidRefreshToken = "auth.invalid_refresh_token";
    }

    public static class Profile
    {
        public const string NotFound = "profile.not_found";
        public const string NotAStudent = "profile.not_a_student";
        public const string NotATeacher = "profile.not_a_teacher";
        public const string CurriculumNotAvailable = "profile.curriculum_not_available";
        public const string GradeNotInCurriculum = "profile.grade_not_in_curriculum";
        public const string StreamNotInCurriculum = "profile.stream_not_in_curriculum";
        public const string SubjectNotInGrade = "profile.subject_not_in_grade";
    }

    public static class Course
    {
        public const string NotFound = "course.not_found";
        public const string InvalidCatalogSelection = "course.invalid_catalog_selection";
        public const string NoPublishedLesson = "course.no_published_lesson";
        public const string HasStudents = "course.has_students";
        public const string StatusChangeNotAllowed = "course.status_change_not_allowed";
        public const string UnitNotFound = "course.unit_not_found";
        public const string LessonNotFound = "course.lesson_not_found";
        public const string ResourceNotFound = "course.resource_not_found";
        public const string OnlyDraftCanBeDeleted = "course.only_draft_can_be_deleted";
        public const string InvalidLink = "course.invalid_link";
        public const string InvalidDocument = "course.invalid_document";
        public const string DocumentTooLarge = "course.document_too_large";
    }

    public static class Video
    {
        public const string InvalidUrl = "video.invalid_url";
        public const string NotFound = "video.not_found";
        public const string NotEmbeddable = "video.not_embeddable";
        public const string Unavailable = "video.unavailable";
        public const string ProviderUnavailable = "video.provider_unavailable";
    }

    public static class Enrollment
    {
        public const string NotFound = "enrollment.not_found";
        public const string AlreadyRequested = "enrollment.already_requested";
        public const string NotAcceptingStudents = "enrollment.not_accepting_students";
        public const string InvalidTransition = "enrollment.invalid_transition";
        public const string NotEnrolled = "enrollment.not_enrolled";
    }

    public static class Teacher
    {
        public const string NotFound = "teacher.not_found";
    }

    public static class Catalog
    {
        public const string InvalidCountryCode = "catalog.invalid_country_code";
        public const string CountryNotFound = "catalog.country_not_found";
        public const string CurriculumNotFound = "catalog.curriculum_not_found";
        public const string GradeLevelNotFound = "catalog.grade_level_not_found";
    }
}
