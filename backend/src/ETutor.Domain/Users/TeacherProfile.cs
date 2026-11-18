namespace ETutor.Domain.Users;

public enum TeacherVerificationStatus : byte
{
    Pending = 0,
    Verified = 1,
    Rejected = 2,
}

/// <summary>Teacher-specific profile. Unverified teachers are hidden from student discovery (decision D5).</summary>
public sealed class TeacherProfile
{
    public Guid UserId { get; set; }

    public required string Headline { get; set; }

    public string Bio { get; set; } = string.Empty;

    public TeacherVerificationStatus VerificationStatus { get; set; } = TeacherVerificationStatus.Pending;

    /// <summary>Set when the status becomes <see cref="TeacherVerificationStatus.Verified"/>; null otherwise.</summary>
    public DateTime? VerifiedAtUtc { get; set; }

    public bool IsAcceptingStudents { get; set; } = true;

    public UserProfile? User { get; set; }
}
