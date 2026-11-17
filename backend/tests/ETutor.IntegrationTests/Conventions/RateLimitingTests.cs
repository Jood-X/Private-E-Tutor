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
