using ETutor.Domain.Enrollments;

namespace ETutor.Application.Enrollments;

public sealed record EnrollmentRequest(string? Message);

public sealed record DecisionRequest(string? Note);

/// <summary>Filter for the teacher's request queue. All filters are optional.</summary>
public sealed record EnrollmentQuery(EnrollmentStatus? Status = null, Guid? CourseId = null);

/// <summary>A student's own enrollment, as listed in "My learning".</summary>
public sealed record MyEnrollmentDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    string SubjectName,
    string TeacherName,
    EnrollmentStatus Status,
    string? DecisionNote,
    DateTime RequestedAtUtc,
    DateTime? DecidedAtUtc);

/// <summary>An enrollment seen by the course's teacher.</summary>
public sealed record TeacherEnrollmentDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    Guid StudentId,
    string StudentName,
    string StudentGradeName,
    EnrollmentStatus Status,
    string? RequestMessage,
    DateTime RequestedAtUtc,
    DateTime? DecidedAtUtc,
    DateTime? LastActivityAtUtc);
