using ETutor.Application.Abstractions;
using ETutor.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ETutor.UnitTests.Infrastructure;

public sealed class TokenServiceTests
{
    private static readonly DateTime Now = new(2026, 11, 18, 10, 0, 0, DateTimeKind.Utc);

    private static readonly JwtOptions Settings = new()
    {
        Issuer = "etutor-api",
        Audience = "etutor-app",
        SigningKey = "unit-test-signing-key-0123456789-abcdefghij",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 30,
    };

    private static TokenService CreateService() => new(Options.Create(Settings), new FixedClock(Now));

    [Fact]
    public async Task AccessToken_IsSignedAndCarriesSubjectRolesAndExpiry()
    {
        var account = new IdentityAccount(Guid.NewGuid(), "lina@etutor.test", ["Student"]);

        var token = CreateService().CreateAccessToken(account);

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, new TokenValidationParameters
        {
            ValidIssuer = Settings.Issuer,
            ValidAudience = Settings.Audience,
            IssuerSigningKey = Settings.CreateSigningKey(),
            ValidateLifetime = false,
        });
        Assert.True(validation.IsValid, validation.Exception?.Message);
        Assert.Equal(account.UserId.ToString(), validation.Claims[JwtRegisteredClaimNames.Sub]);
        Assert.Equal("Student", validation.Claims[JwtOptions.RoleClaimType]);
        Assert.Equal(Now.AddMinutes(15), token.ExpiresAtUtc);
    }

    [Fact]
    public void RefreshToken_IsRandom_AndOnlyItsHashIsReturnedForStorage()
    {
        var service = CreateService();

        var first = service.CreateRefreshToken();
        var second = service.CreateRefreshToken();

        Assert.NotEqual(first.Value, second.Value);
        Assert.True(first.Value.Length >= 43, "at least 256 bits of entropy, base64url-encoded");
        Assert.Equal(32, first.Hash.Length);
        Assert.Equal(first.Hash, service.HashRefreshToken(first.Value));
        Assert.Equal(Now.AddDays(30), first.ExpiresAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData("too-short-key")]
    public void Options_RejectSigningKeysShorterThan256Bits(string key)
    {
        var options = new JwtOptions { Issuer = "i", Audience = "a", SigningKey = key };

        var errors = options.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(options)).ToList();

        Assert.Single(errors);
    }

    private sealed class FixedClock(DateTime now) : IClock
    {
        public DateTime UtcNow => now;
    }
}
