using System.Collections.Concurrent;
using ETutor.Application.Abstractions;
using ETutor.Infrastructure.Videos;

namespace ETutor.IntegrationTests.Infrastructure;

/// <summary>
/// Stands in for YouTube: parses links with the real parser and answers lookups by the video ID's prefix, so tests
/// sharing one host never interfere. "missing" → not found, "noembed" → embedding off, "outage" → provider down;
/// anything else is a 10-minute video titled "Lecture {id}".
/// </summary>
public sealed class FakeVideoProvider : IVideoProvider
{
    private readonly ConcurrentDictionary<string, int> lookups = new();

    public string Name => YouTubeVideoProvider.ProviderName;

    public bool TryParseAssetId(string input, out string assetId) => YouTubeVideoProvider.TryParse(input, out assetId);

    public Task<VideoLookup> LookupAsync(string assetId, CancellationToken cancellationToken)
    {
        lookups.AddOrUpdate(assetId, 1, (_, count) => count + 1);
        VideoLookup result = assetId switch
        {
            _ when assetId.StartsWith("missing", StringComparison.Ordinal) => new(VideoLookupStatus.NotFound),
            _ when assetId.StartsWith("noembed", StringComparison.Ordinal) => new(VideoLookupStatus.NotEmbeddable),
            _ when assetId.StartsWith("outage", StringComparison.Ordinal) => new(VideoLookupStatus.ProviderUnavailable),
            _ => VideoLookup.Found(new VideoMetadata($"Lecture {assetId}", 600, $"https://i.ytimg.com/vi/{assetId}/mqdefault.jpg")),
        };
        return Task.FromResult(result);
    }

    public int LookupCount(string assetId) => lookups.GetValueOrDefault(assetId);

    /// <summary>A fresh, valid 11-character ID with the given behaviour prefix.</summary>
    public static string NewId(string prefix = "v") => (prefix + Guid.NewGuid().ToString("N"))[..11];
}
