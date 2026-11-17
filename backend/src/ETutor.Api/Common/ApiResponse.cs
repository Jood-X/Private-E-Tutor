using ETutor.Application.Common.Results;

namespace ETutor.Api.Common;

/// <summary>
/// The response envelope used by every JSON endpoint (docs/api-conventions.md).
/// Exceptions: file downloads, health checks and the OpenAPI document.
/// </summary>
public sealed record ApiResponse<T>(
    bool Success,
    string? Message,
    T? Data,
    IReadOnlyList<ApiError>? Errors,
    string TraceId);

/// <summary>One error. <c>Code</c> is stable and machine-readable; <c>Field</c> is set for input errors.</summary>
public sealed record ApiError(string Code, string Message, string? Field = null);

public static class ApiResponse
{
    public static ApiResponse<T> Ok<T>(T data, string traceId, string? message = null) =>
        new(true, message, data, null, traceId);

    public static ApiResponse<T> Fail<T>(IReadOnlyList<ApiError> errors, string traceId)
    {
        ArgumentNullException.ThrowIfNull(errors);
        return new ApiResponse<T>(false, errors[0].Message, default, errors, traceId);
    }

    public static ApiResponse<object> Fail(string code, string message, string traceId) =>
        Fail<object>([new ApiError(code, message)], traceId);

    public static IReadOnlyList<ApiError> ToApiErrors(IEnumerable<Error> errors) =>
        errors.Select(error => new ApiError(error.Code, error.Message, error.Field)).ToList();
}
