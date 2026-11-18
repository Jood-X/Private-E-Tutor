using System.Net;
using ETutor.IntegrationTests.Infrastructure;

namespace ETutor.IntegrationTests.Conventions;

/// <summary>A host with a tiny limit so the third request in the window is rejected.</summary>
public sealed class LowLimitApiFactory : ApiFactory
{
    protected override int RateLimitPermits => 2;

    /// <summary>These tests never reach the database, so it is not rebuilt (avoids racing the shared fixture).</summary>
    public override Task InitializeAsync() => Task.CompletedTask;
}

public sealed class RateLimitingTests(LowLimitApiFactory factory) : IClassFixture<LowLimitApiFactory>
{
    [Fact]
    public async Task ExceedingTheLimit_Returns429Envelope_WithRetryAfter()
    {
        var client = factory.CreateClient();

        await client.GetAsync("/api/v1/does-not-exist");
        await client.GetAsync("/api/v1/does-not-exist");
        var rejected = await client.GetAsync("/api/v1/does-not-exist");

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal("common.rate_limited", (await rejected.ReadEnvelopeAsync<object>()).Errors![0].Code);
        Assert.True(rejected.Headers.Contains("Retry-After"));
    }
}

/// <summary>Global limit high, credential-endpoint limit 2: isolates the stricter auth policy.</summary>
public sealed class LowAuthLimitApiFactory : ApiFactory
{
    protected override int AuthRateLimitPermits => 2;

    public override Task InitializeAsync() => Task.CompletedTask;
}

public sealed class AuthRateLimitingTests(LowAuthLimitApiFactory factory) : IClassFixture<LowAuthLimitApiFactory>
{
    [Fact]
    public async Task CredentialEndpoints_HaveTheirOwnStricterLimit()
    {
        var client = factory.CreateClient();

        // Other endpoints are unaffected by the auth limit (anonymous unknown route: 401, never 429).
        await client.GetAsync("/api/v1/does-not-exist");
        await client.GetAsync("/api/v1/does-not-exist");
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/does-not-exist")).StatusCode);

        HttpResponseMessage last = new();
        for (var attempt = 0; attempt < 3; attempt++)
        {
            using var body = new StringContent(
                "{\"email\":\"x@etutor.test\",\"password\":\"whatever-1\"}", System.Text.Encoding.UTF8, "application/json");
            last = await client.PostAsync("/api/v1/auth/logout", body);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, last.StatusCode);
        Assert.Equal("common.rate_limited", (await last.ReadEnvelopeAsync<object>()).Errors![0].Code);
    }
}
