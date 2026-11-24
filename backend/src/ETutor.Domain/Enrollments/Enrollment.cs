using ETutor.Domain.Content;
using ETutor.Domain.Users;

namespace ETutor.Domain.Enrollments;

public enum EnrollmentStatus : byte
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Withdrawn = 3,
    Revoked = 4,
}

/// <summary>
/// The teacher–student relationship for one course. Terminal rows are kept as history; a new request
/// creates a new row. At most one Pending or Approved row per (course, student) (filtered unique index).
/// </summary>
public sealed class Enrollment
{
    public Guid Id { get; set; }

    public Guid CourseId { get; set; }

    public Guid StudentId { get; set; }

    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Pending;

    public string? RequestMessage { get; set; }

    public string? DecisionNote { get; set; }

    public DateTime RequestedAtUtc { get; set; }

    public DateTime? DecidedAtUtc { get; set; }

    /// <summary>Last learning activity; null until the student does something in the course (Phase 5).</summary>
    public DateTime? LastActivityAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public Course? Course { get; set; }

    public StudentProfile? Student { get; set; }
}

/// <summary>
/// The allowed enrollment state changes, and who may make them. Pure rules, unit-tested.
/// </summary>
public static class EnrollmentTransitions
{
    public enum Actor
    {
        Student,
        Teacher,
    }

    public static bool IsActive(EnrollmentStatus status) =>
        status is EnrollmentStatus.Pending or EnrollmentStatus.Approved;

    public static bool CanMove(EnrollmentStatus from, EnrollmentStatus to, Actor actor) => (from, to, actor) switch
    {
        (EnrollmentStatus.Pending, EnrollmentStatus.Approved, Actor.Teacher) => true,
        (EnrollmentStatus.Pending, EnrollmentStatus.Rejected, Actor.Teacher) => true,
        (EnrollmentStatus.Approved, EnrollmentStatus.Revoked, Actor.Teacher) => true,
        (EnrollmentStatus.Pending or EnrollmentStatus.Approved, EnrollmentStatus.Withdrawn, Actor.Student) => true,
        _ => false,
    };
}
