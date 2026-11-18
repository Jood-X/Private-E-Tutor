namespace ETutor.Application.Abstractions;

public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);

/// <summary>A new refresh token: <see cref="Value"/> goes to the client once; only <see cref="Hash"/> is stored.</summary>
public sealed record NewRefreshToken(string Value, byte[] Hash, DateTime ExpiresAtUtc);

/// <summary>Creates signed access tokens and random refresh tokens (implemented in Infrastructure).</summary>
public interface ITokenService
{
    AccessToken CreateAccessToken(IdentityAccount account);

    NewRefreshToken CreateRefreshToken();

    /// <summary>Hashes a presented refresh token so it can be looked up; never store or log the raw value.</summary>
    byte[] HashRefreshToken(string refreshToken);
}
