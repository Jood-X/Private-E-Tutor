using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ETutor.Application.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ETutor.Infrastructure.Identity;

/// <summary>Signed JWT access tokens (HMAC-SHA256) and opaque random refresh tokens.</summary>
internal sealed class TokenService(IOptions<JwtOptions> options, IClock clock) : ITokenService
{
    private const int RefreshTokenBytes = 32;

    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken CreateAccessToken(IdentityAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);
        var settings = options.Value;
        var now = clock.UtcNow;
        var expires = now.AddMinutes(settings.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, account.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, account.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };
        claims.AddRange(account.Roles.Select(role => new Claim(JwtOptions.RoleClaimType, role)));

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = new SigningCredentials(settings.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
        });
        return new AccessToken(token, expires);
    }

    public NewRefreshToken CreateRefreshToken()
    {
        var value = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenBytes));
        return new NewRefreshToken(value, HashRefreshToken(value), clock.UtcNow.AddDays(options.Value.RefreshTokenDays));
    }

    public byte[] HashRefreshToken(string refreshToken)
    {
        ArgumentNullException.ThrowIfNull(refreshToken);
        return SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
    }
}
