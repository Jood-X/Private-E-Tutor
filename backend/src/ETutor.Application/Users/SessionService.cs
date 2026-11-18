using ETutor.Application.Abstractions;
using ETutor.Application.Common;
using ETutor.Application.Common.Results;
using ETutor.Application.Common.Validation;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ETutor.Application.Users;

/// <summary>
/// Sign-in, token refresh (rotation with reuse detection) and sign-out.
/// Refresh tokens are single-use: each refresh revokes the presented token and issues a new one in the
/// same family. Presenting an already-used token means it was copied, so the whole family is revoked.
/// </summary>
public sealed class SessionService(
    IAppDbContext db,
    IIdentityService identity,
    ITokenService tokens,
    TokenIssuer tokenIssuer,
    ProfileQueries profiles,
    IClock clock,
    IValidator<LoginRequest> loginValidator,
    ILogger<SessionService> logger)
{
    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await loginValidator.ValidateToErrorsAsync(request, cancellationToken) is { } invalid)
        {
            return Result<AuthResponse>.Failure(invalid);
        }

        var account = await identity.CheckCredentialsAsync(request.Email, request.Password, cancellationToken);
        if (!account.IsSuccess)
        {
            UserLog.LoginFailed(logger, account.Errors[0].Code);
            return Result<AuthResponse>.Failure(account.Errors);
        }

        UserLog.LoginSucceeded(logger, account.Value.UserId);
        return await BuildResponseAsync(account.Value, familyId: Guid.NewGuid(), cancellationToken);
    }

    public async Task<Result<AuthResponse>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return InvalidRefreshToken("missing token");
        }

        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var stored = await db.RefreshTokens.AsNoTracking()
            .FirstOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);
        var now = clock.UtcNow;

        if (stored is null)
        {
            return InvalidRefreshToken("unknown token");
        }

        if (stored.RevokedAtUtc is not null)
        {
            await RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            UserLog.RefreshReuseDetected(logger, stored.UserId, stored.FamilyId);
            return InvalidRefreshToken("token reuse");
        }

        if (stored.ExpiresAtUtc <= now)
        {
            return InvalidRefreshToken("expired token");
        }

        var account = await identity.FindActiveAccountAsync(stored.UserId, cancellationToken);
        if (account is null)
        {
            await RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            return InvalidRefreshToken("account unavailable");
        }

        var result = await db.ExecuteInTransactionAsync(async ct =>
        {
            // Atomic "use once": only one concurrent request can flip RevokedAtUtc from null.
            var claimed = await db.RefreshTokens
                .Where(token => token.Id == stored.Id && token.RevokedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAtUtc, now), ct);
            if (claimed == 0)
            {
                UserLog.RefreshReuseDetected(logger, stored.UserId, stored.FamilyId);
                return InvalidRefreshToken("concurrent reuse");
            }

            return await BuildResponseAsync(account, stored.FamilyId, ct, replacedTokenId: stored.Id);
        }, cancellationToken);

        if (!result.IsSuccess && result.Errors[0].Code == ErrorCodes.Auth.InvalidRefreshToken)
        {
            // The failed transaction rolled back, so revoke the family outside it to make it stick.
            await RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
        }

        return result;
    }

    /// <summary>Signs out the session the refresh token belongs to. Idempotent and silent about unknown tokens.</summary>
    public async Task LogoutAsync(RefreshRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return;
        }

        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var familyId = await db.RefreshTokens.AsNoTracking()
            .Where(token => token.TokenHash == hash)
            .Select(token => (Guid?)token.FamilyId)
            .FirstOrDefaultAsync(cancellationToken);
        if (familyId is { } family)
        {
            await RevokeFamilyAsync(family, clock.UtcNow, cancellationToken);
            UserLog.LoggedOut(logger, family);
        }
    }

    private async Task<Result<AuthResponse>> BuildResponseAsync(
        IdentityAccount account, Guid familyId, CancellationToken cancellationToken, Guid? replacedTokenId = null)
    {
        var issued = await tokenIssuer.IssueAsync(account, familyId, cancellationToken);
        if (replacedTokenId is { } oldId)
        {
            await db.RefreshTokens.Where(token => token.Id == oldId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.ReplacedByTokenId, issued.RefreshTokenId), cancellationToken);
        }

        var me = await profiles.GetMeAsync(account, cancellationToken);
        return me.IsSuccess
            ? new AuthResponse(issued.Access.Value, issued.Access.ExpiresAtUtc, issued.Refresh.Value, issued.Refresh.ExpiresAtUtc, me.Value)
            : Result<AuthResponse>.Failure(me.Errors);
    }

    private Task<int> RevokeFamilyAsync(Guid familyId, DateTime now, CancellationToken cancellationToken) =>
        db.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAtUtc, now), cancellationToken);

    private Result<AuthResponse> InvalidRefreshToken(string reason)
    {
        UserLog.RefreshRejected(logger, reason);
        return Error.Unauthorized(ErrorCodes.Auth.InvalidRefreshToken, "Your session has expired. Please sign in again.");
    }
}
