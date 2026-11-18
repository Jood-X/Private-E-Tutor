using ETutor.Application.Common.Results;

namespace ETutor.Application.Abstractions;

/// <summary>An identity account as the application sees it: no password data, ever.</summary>
public sealed record IdentityAccount(Guid UserId, string Email, IReadOnlyList<string> Roles);

/// <summary>
/// Port to the credential store (ASP.NET Core Identity in Infrastructure): password hashing,
/// password policy, lockout and roles.
/// </summary>
public interface IIdentityService
{
    /// <summary>Creates an account with one role. Fails with <c>auth.email_taken</c> or <c>auth.weak_password</c>.</summary>
    Task<Result<IdentityAccount>> CreateAccountAsync(
        string email, string password, string role, CancellationToken cancellationToken);

    /// <summary>
    /// Verifies a password and applies lockout. Unknown email and wrong password produce the same
    /// error so the response does not reveal which accounts exist.
    /// </summary>
    Task<Result<IdentityAccount>> CheckCredentialsAsync(string email, string password, CancellationToken cancellationToken);

    /// <summary>Returns the account if it exists and is not locked out.</summary>
    Task<IdentityAccount?> FindActiveAccountAsync(Guid userId, CancellationToken cancellationToken);
}
