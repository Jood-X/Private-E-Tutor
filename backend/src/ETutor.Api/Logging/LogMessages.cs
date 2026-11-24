using ETutor.Application.Common.Results;

namespace ETutor.Api.Logging;

/// <summary>
/// Source-generated log messages with stable event IDs and names (docs/logging.md).
/// Never pass passwords, tokens, or request bodies to these methods.
/// </summary>
internal static partial class LogMessages
{
    [LoggerMessage(EventId = 1000, EventName = "request.unhandled_exception", Level = LogLevel.Error,
        Message = "Unhandled exception while processing {Method} {Path}")]
    public static partial void UnhandledException(ILogger logger, Exception exception, string method, string path);

    [LoggerMessage(EventId = 1001, EventName = "request.validation_failed", Level = LogLevel.Information,
        Message = "Request validation failed for fields: {Fields}")]
    public static partial void ValidationFailed(ILogger logger, string fields);

    [LoggerMessage(EventId = 1002, EventName = "request.failed", Level = LogLevel.Information,
        Message = "Request failed with {ErrorCode} ({ErrorType})")]
    public static partial void RequestFailed(ILogger logger, string errorCode, ErrorType errorType);

    [LoggerMessage(EventId = 1003, EventName = "request.rate_limited", Level = LogLevel.Warning,
        Message = "Rate limit exceeded for {Method} {Path}")]
    public static partial void RateLimited(ILogger logger, string method, string path);

    [LoggerMessage(EventId = 1004, EventName = "request.bad_request", Level = LogLevel.Information,
        Message = "Malformed request rejected: {Reason}")]
    public static partial void BadRequest(ILogger logger, string reason);

    [LoggerMessage(EventId = 1005, EventName = "request.concurrency_conflict", Level = LogLevel.Warning,
        Message = "Concurrent update rejected for {Method} {Path}")]
    public static partial void ConcurrencyConflict(ILogger logger, string method, string path);
}
