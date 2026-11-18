using ETutor.Application.Abstractions;
using ETutor.Domain.Users;

namespace ETutor.Application.Users;

public sealed record IssuedTokens(AccessToken Access, NewRefreshToken Refresh, Guid RefreshTokenId);

/// <summary>Issues an access token and stores the hash of a new refresh token in the given family.</summary>
public sealed class TokenIssuer(IAppDbContext db, ITokenService tokens, IClock clock)
{
    public async Task<IssuedTokens> IssueAsync(IdentityAccount account, Guid familyId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);
        var access = tokens.CreateAccessToken(account);
        var refresh = tokens.CreateRefreshToken();
        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = account.UserId,
            TokenHash = refresh.Hash,
            FamilyId = familyId,
            CreatedAtUtc = clock.UtcNow,
            ExpiresAtUtc = refresh.ExpiresAtUtc,
        };
        db.RefreshTokens.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return new IssuedTokens(access, refresh, entity.Id);
    }
}
