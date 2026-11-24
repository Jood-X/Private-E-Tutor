using ETutor.Domain.Enrollments;
using Microsoft.Extensions.Logging;

namespace ETutor.Application.Enrollments;

/// <summary>Enrollment events (EventId range 3000-3099). IDs only; never messages or names.</summary>
internal static partial class EnrollmentLog
{
    [LoggerMessage(EventId = 3000, EventName = "enrollment.requested", Level = LogLevel.Information,
        Message = "Enrollment {EnrollmentId} requested for course {CourseId}")]
    public static partial void Requested(ILogger logger, Guid enrollmentId, Guid courseId);

    [LoggerMessage(EventId = 3001, EventName = "enrollment.duplicate_request", Level = LogLevel.Information,
        Message = "Duplicate enrollment request rejected for course {CourseId}")]
    public static partial void DuplicateRequest(ILogger logger, Guid courseId);

    [LoggerMessage(EventId = 3002, EventName = "enrollment.decided", Level = LogLevel.Information,
        Message = "Enrollment {EnrollmentId} moved to {Status}")]
    public static partial void Decided(ILogger logger, Guid enrollmentId, EnrollmentStatus status);
}
