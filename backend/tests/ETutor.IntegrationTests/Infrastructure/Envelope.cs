using System.Net.Http.Json;
using System.Text.Json;

namespace ETutor.IntegrationTests.Infrastructure;

/// <summary>Client-side view of the response envelope, deserialized exactly as a client would.</summary>
public sealed record Envelope<T>(bool Success, string? Message, T? Data, List<EnvelopeError>? Errors, string TraceId);

public sealed record EnvelopeError(string Code, string Message, string? Field);

public static class EnvelopeExtensions
{
    // Same conventions as the API: camelCase and enums as strings.
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    public static async Task<Envelope<T>> ReadEnvelopeAsync<T>(this HttpResponseMessage response)
    {
        var envelope = await response.Content.ReadFromJsonAsync<Envelope<T>>(Options);
        Assert.NotNull(envelope);
        return envelope;
    }
}
