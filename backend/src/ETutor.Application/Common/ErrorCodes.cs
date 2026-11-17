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

    public static class Catalog
    {
        public const string InvalidCountryCode = "catalog.invalid_country_code";
        public const string CountryNotFound = "catalog.country_not_found";
        public const string CurriculumNotFound = "catalog.curriculum_not_found";
        public const string GradeLevelNotFound = "catalog.grade_level_not_found";
    }
}
