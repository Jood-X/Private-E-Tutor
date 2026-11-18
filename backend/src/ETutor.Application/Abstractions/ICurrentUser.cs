namespace ETutor.Application.Abstractions;

/// <summary>The authenticated caller of the current request.</summary>
public interface ICurrentUser
{
    /// <summary>The caller's user ID, or null for anonymous requests.</summary>
    Guid? UserId { get; }

    bool IsInRole(string role);
}
