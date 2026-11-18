using ETutor.Application.Abstractions;
using ETutor.Application.Common;
using ETutor.Application.Common.Results;
using Microsoft.AspNetCore.Identity;

namespace ETutor.Infrastructure.Identity;

/// <summary><see cref="IIdentityService"/> backed by ASP.NET Core Identity (PBKDF2 hashing, lockout, roles).</summary>
internal sealed class IdentityService(UserManager<ApplicationUser> users) : IIdentityService
{
    /// <summary>
    /// Hash checked when the email is unknown, so a failed sign-in takes about as long whether or not
    /// the account exists (reduces account enumeration through timing).
    /// </summary>
    private static readonly Lazy<string> DummyHash = new(() =>
        new PasswordHasher<ApplicationUser>().HashPassword(new ApplicationUser(), Guid.NewGuid().ToString()));

    public async Task<Result<IdentityAccount>> CreateAccountAsync(
        string email, string password, string role, CancellationToken cancellationToken)
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = email.Trim(), Email = email.Trim() };
        var created = await users.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            return Result<IdentityAccount>.Failure(MapErrors(created.Errors));
        }

        var roleAdded = await users.AddToRoleAsync(user, role);
        if (!roleAdded.Succeeded)
        {
            throw new InvalidOperationException($"Could not assign role '{role}': {string.Join(", ", roleAdded.Errors.Select(e => e.Code))}");
        }

        return new IdentityAccount(user.Id, user.Email!, [role]);
    }

    public async Task<Result<IdentityAccount>> CheckCredentialsAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(email.Trim());
        if (user is null)
        {
            _ = new PasswordHasher<ApplicationUser>().VerifyHashedPassword(new ApplicationUser(), DummyHash.Value, password);
            return InvalidCredentials();
        }

        if (await users.IsLockedOutAsync(user))
        {
            return LockedOut();
        }

        if (!await users.CheckPasswordAsync(user, password))
        {
            await users.AccessFailedAsync(user);
            return await users.IsLockedOutAsync(user) ? LockedOut() : InvalidCredentials();
        }

        await users.ResetAccessFailedCountAsync(user);
        return await ToAccountAsync(user);
    }

    public async Task<IdentityAccount?> FindActiveAccountAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId.ToString());
        if (user is null || await users.IsLockedOutAsync(user))
        {
            return null;
        }

        return await ToAccountAsync(user);
    }

    private async Task<IdentityAccount> ToAccountAsync(ApplicationUser user) =>
        new(user.Id, user.Email!, [.. await users.GetRolesAsync(user)]);

    private static List<Error> MapErrors(IEnumerable<IdentityError> errors)
    {
        var list = errors.ToList();
        if (list.Any(error => error.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName)))
        {
            return [new Error(ErrorCodes.Auth.EmailTaken, "An account with this email already exists.", ErrorType.Conflict, "email")];
        }

        var passwordErrors = list.Where(error => error.Code.StartsWith("Password", StringComparison.Ordinal)).ToList();
        if (passwordErrors.Count > 0)
        {
            return [Error.Validation(ErrorCodes.Auth.WeakPassword, string.Join(" ", passwordErrors.Select(e => e.Description)), "password")];
        }

        return list.Select(error => Error.Validation(ErrorCodes.Common.ValidationFailed, error.Description, "email")).ToList();
    }

    private static Error InvalidCredentials() =>
        Error.Unauthorized(ErrorCodes.Auth.InvalidCredentials, "The email or password is incorrect.");

    private static Error LockedOut() =>
        Error.Unauthorized(ErrorCodes.Auth.LockedOut, "Too many failed attempts. Try again in a few minutes.");
}
