using Microsoft.Extensions.Logging;

namespace ETutor.Application.Users;

/// <summary>
/// Security and account events (EventId range 2000-2099). Only opaque user IDs are logged:
/// never emails, passwords or tokens.
/// </summary>
internal static partial class UserLog
{
    [LoggerMessage(EventId = 2000, EventName = "auth.registered", Level = LogLevel.Information,
        Message = "Account {UserId} registered as {Role}")]
    public static partial void Registered(ILogger logger, Guid userId, string role);

    [LoggerMessage(EventId = 2001, EventName = "auth.login.succeeded", Level = LogLevel.Information,
        Message = "User {UserId} signed in")]
    public static partial void LoginSucceeded(ILogger logger, Guid userId);

    [LoggerMessage(EventId = 2002, EventName = "auth.login.failed", Level = LogLevel.Warning,
        Message = "Sign-in failed with {ErrorCode}")]
    public static partial void LoginFailed(ILogger logger, string errorCode);

    [LoggerMessage(EventId = 2003, EventName = "auth.refresh.reuse_detected", Level = LogLevel.Warning,
        Message = "Refresh token reuse detected for user {UserId}; token family {FamilyId} revoked")]
    public static partial void RefreshReuseDetected(ILogger logger, Guid userId, Guid familyId);

    [LoggerMessage(EventId = 2004, EventName = "auth.refresh.rejected", Level = LogLevel.Information,
        Message = "Refresh rejected: {Reason}")]
    public static partial void RefreshRejected(ILogger logger, string reason);

    [LoggerMessage(EventId = 2005, EventName = "auth.logout", Level = LogLevel.Information,
        Message = "Token family {FamilyId} signed out")]
    public static partial void LoggedOut(ILogger logger, Guid familyId);
}
