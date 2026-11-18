namespace ETutor.Domain.Users;

/// <summary>Platform roles. Names are stored in the identity store and carried in access tokens.</summary>
public enum UserRole
{
    Student,
    Teacher,

    /// <summary>Reserved for future administration; no endpoints use it yet.</summary>
    Admin,
}

public static class RoleNames
{
    public const string Student = nameof(UserRole.Student);
    public const string Teacher = nameof(UserRole.Teacher);
    public const string Admin = nameof(UserRole.Admin);
}
