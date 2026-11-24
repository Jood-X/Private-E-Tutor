using ETutor.Application.Abstractions;

namespace ETutor.Application.Common;

public static class CurrentUserExtensions
{
    /// <summary>
    /// The caller's ID on endpoints that require authentication. A missing ID there is a programming error
    /// (the endpoint lacks [Authorize]), so it throws instead of returning a business error.
    /// </summary>
    public static Guid RequireUserId(this ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        return currentUser.UserId
            ?? throw new InvalidOperationException("This operation requires an authenticated user.");
    }
}
