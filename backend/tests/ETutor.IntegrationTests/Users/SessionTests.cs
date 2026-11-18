using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using ETutor.Application.Users;
using ETutor.IntegrationTests.Infrastructure;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ETutor.IntegrationTests.Users;

[Collection(ApiTestGroup.Name)]
public sealed class SessionTests(ApiFactory factory)
{
    private const string LoginRoute = "/api/v1/auth/login";
    private const string RefreshRoute = "/api/v1/auth/refresh";
    private const string MeRoute = "/api/v1/me";

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsTokensThatAuthorizeMe()
    {
        var registered = await factory.CreateClient().RegisterStudentAsync();

        var response = await factory.CreateClient().PostAsJsonAsync(LoginRoute, new LoginRequest(registered.User.Email, AuthClient.Password));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = (await response.ReadEnvelopeAsync<AuthResponse>()).Data!;
        var me = await factory.Authorized(auth.AccessToken).GetAsync(MeRoute);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(registered.User.Id, (await me.ReadEnvelopeAsync<MeDto>()).Data!.Id);
    }

    [Fact]
    public async Task Login_UnknownEmailAndWrongPassword_ReturnTheSameError()
    {
        var registered = await factory.CreateClient().RegisterStudentAsync();

        var wrongPassword = await factory.CreateClient().PostAsJsonAsync(LoginRoute, new LoginRequest(registered.User.Email, "Wrong-password-1"));
        var unknownEmail = await factory.CreateClient().PostAsJsonAsync(LoginRoute, new LoginRequest(AuthClient.UniqueEmail(), "Wrong-password-1"));

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        var first = (await wrongPassword.ReadEnvelopeAsync<object>()).Errors![0];
        var second = (await unknownEmail.ReadEnvelopeAsync<object>()).Errors![0];
        Assert.Equal("auth.invalid_credentials", first.Code);
        Assert.Equal((first.Code, first.Message), (second.Code, second.Message));
    }

    [Fact]
    public async Task Login_LocksTheAccountAfterFiveFailures_EvenForTheRightPassword()
    {
        var registered = await factory.CreateClient().RegisterStudentAsync();
        var client = factory.CreateClient();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await client.PostAsJsonAsync(LoginRoute, new LoginRequest(registered.User.Email, "Wrong-password-1"));
        }

        var response = await client.PostAsJsonAsync(LoginRoute, new LoginRequest(registered.User.Email, AuthClient.Password));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("auth.locked_out", (await response.ReadEnvelopeAsync<object>()).Errors![0].Code);
    }

    [Fact]
    public async Task Refresh_RotatesTokens_AndReusingAnOldTokenRevokesTheWholeSession()
    {
        var registered = await factory.CreateClient().RegisterStudentAsync();
        var client = factory.CreateClient();

        var first = await client.PostAsJsonAsync(RefreshRoute, new RefreshRequest(registered.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var rotated = (await first.ReadEnvelopeAsync<AuthResponse>()).Data!;
        Assert.NotEqual(registered.RefreshToken, rotated.RefreshToken);

        // Replaying the old token (e.g. stolen copy) is rejected...
        var replay = await client.PostAsJsonAsync(RefreshRoute, new RefreshRequest(registered.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        Assert.Equal("auth.invalid_refresh_token", (await replay.ReadEnvelopeAsync<object>()).Errors![0].Code);

        // ...and also kills the legitimate newer token of the same session.
        var afterReuse = await client.PostAsJsonAsync(RefreshRoute, new RefreshRequest(rotated.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, afterReuse.StatusCode);
    }

    [Fact]
    public async Task Refresh_UnknownToken_Returns401()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(RefreshRoute, new RefreshRequest("not-a-real-token"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesTheRefreshToken()
    {
        var registered = await factory.CreateClient().RegisterStudentAsync();
        var client = factory.CreateClient();

        var logout = await client.PostAsJsonAsync("/api/v1/auth/logout", new RefreshRequest(registered.RefreshToken));
        var refresh = await client.PostAsJsonAsync(RefreshRoute, new RefreshRequest(registered.RefreshToken));

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401Envelope()
    {
        var response = await factory.CreateClient().GetAsync(MeRoute);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("common.unauthorized", (await response.ReadEnvelopeAsync<object>()).Errors![0].Code);
    }

    [Fact]
    public async Task Me_WithTamperedToken_Returns401()
    {
        var registered = await factory.CreateClient().RegisterStudentAsync();
        var parts = registered.AccessToken.Split('.');
        var tampered = $"{parts[0]}.{parts[1]}x.{parts[2]}";

        var response = await factory.Authorized(tampered).GetAsync(MeRoute);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("a-completely-different-signing-key-0123456789", 15)] // forged with another key
    [InlineData(ApiFactory.TestSigningKey, -5)]                      // correct key but expired
    public async Task Me_WithForgedOrExpiredToken_Returns401(string signingKey, int expiresInMinutes)
    {
        var registered = await factory.CreateClient().RegisterStudentAsync();
        var now = DateTime.UtcNow;
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "etutor-api",
            Audience = "etutor-app",
            Subject = new ClaimsIdentity([new Claim("sub", registered.User.Id.ToString()), new Claim("role", "Student")]),
            IssuedAt = now.AddMinutes(-20),
            NotBefore = now.AddMinutes(-20),
            Expires = now.AddMinutes(expiresInMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256),
        });

        var response = await factory.Authorized(token).GetAsync(MeRoute);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
