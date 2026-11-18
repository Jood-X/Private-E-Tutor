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

    public static class Catalog
    {
        public const string InvalidCountryCode = "catalog.invalid_country_code";
        public const string CountryNotFound = "catalog.country_not_found";
        public const string CurriculumNotFound = "catalog.curriculum_not_found";
        public const string GradeLevelNotFound = "catalog.grade_level_not_found";
    }
}
