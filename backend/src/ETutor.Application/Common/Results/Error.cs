namespace ETutor.Application.Common.Results;

/// <summary>Category of an expected failure. The API maps each category to one HTTP status code.</summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    BusinessRule,
}

/// <summary>
/// An expected failure. <see cref="Code"/> is a stable, machine-readable key that clients
/// translate into a localized message; <see cref="Message"/> is an English fallback.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type, string? Field = null)
{
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Validation(string code, string message, string? field = null) =>
        new(code, message, ErrorType.Validation, field);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    public static Error BusinessRule(string code, string message) => new(code, message, ErrorType.BusinessRule);
}
