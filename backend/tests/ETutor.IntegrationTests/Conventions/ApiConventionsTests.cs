using System.Net;
using System.Net.Http.Json;
using System.Text;
using ETutor.IntegrationTests.Infrastructure;

namespace ETutor.IntegrationTests.Conventions;

/// <summary>Cross-cutting guarantees from docs/api-conventions.md.</summary>
[Collection(ApiTestGroup.Name)]
public sealed class ApiConventionsTests(ApiFactory factory)
{
    [Fact]
    public async Task UnknownRoute_ForSignedInUser_Returns404Envelope()
    {
        var response = await (await SignedInClientAsync()).GetAsync("/api/v1/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var envelope = await response.ReadEnvelopeAsync<object>();
        Assert.False(envelope.Success);
        Assert.Equal("common.not_found", envelope.Errors![0].Code);
    }

    [Fact]
    public async Task UnknownRoute_ForAnonymousCaller_Returns401_SoRoutesAreNotDiscoverable()
    {
        // Authorization is deny-by-default: without a token, anything that is not explicitly public is 401.
        var response = await factory.CreateClient().GetAsync("/api/v1/does-not-exist");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("common.unauthorized", (await response.ReadEnvelopeAsync<object>()).Errors![0].Code);
    }

    [Fact]
    public async Task WrongMethod_ForSignedInUser_Returns405Envelope()
    {
        var response = await (await SignedInClientAsync()).DeleteAsync("/api/v1/catalog/countries");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal("common.method_not_allowed", (await response.ReadEnvelopeAsync<object>()).Errors![0].Code);
    }

    [Fact]
    public async Task UnhandledException_Returns500_WithoutLeakingInternals()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/test-only/throw");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("SELECT", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InvalidOperationException", body, StringComparison.Ordinal);
        Assert.DoesNotContain("   at ", body, StringComparison.Ordinal);

        var envelope = await response.ReadEnvelopeAsync<object>();
        Assert.Equal("common.unexpected", envelope.Errors![0].Code);
        Assert.False(string.IsNullOrWhiteSpace(envelope.TraceId));
    }

    [Fact]
    public async Task InvalidBody_Returns400_WithOneErrorPerField()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/test-only/echo", new { count = 50 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var envelope = await response.ReadEnvelopeAsync<object>();
        var fields = envelope.Errors!.Select(error => error.Field).ToHashSet();
        Assert.Contains("name", fields);
        Assert.Contains("count", fields);
        Assert.All(envelope.Errors!, error => Assert.Equal("common.validation_failed", error.Code));
    }

    [Fact]
    public async Task MalformedJson_Returns400Envelope()
    {
        using var content = new StringContent("{ not json", Encoding.UTF8, "application/json");

        var response = await factory.CreateClient().PostAsync("/api/v1/test-only/echo", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False((await response.ReadEnvelopeAsync<object>()).Success);
    }

    [Fact]
    public async Task ValidBody_ReturnsSuccessEnvelope_InCamelCase()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/v1/test-only/echo", new { name = "Lina", count = 2 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"success\":true", json, StringComparison.Ordinal);
        Assert.Contains("\"data\":{\"name\":\"Lina\",\"count\":2}", json, StringComparison.Ordinal);
        Assert.Contains("\"traceId\":", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Responses_CarrySecurityAndTraceHeaders()
    {
        var response = await factory.CreateClient().GetAsync("/api/v1/catalog/countries");

        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Matches("^[0-9a-f]{32}$", response.Headers.GetValues("X-Trace-Id").Single());
    }

    [Fact]
    public async Task Health_ReportsHealthy_WhenDatabaseIsReachable()
    {
        var response = await factory.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    private async Task<HttpClient> SignedInClientAsync()
    {
        var auth = await Users.AuthClient.RegisterStudentAsync(factory.CreateClient());
        return Users.AuthClient.Authorized(factory, auth.AccessToken);
    }
}
