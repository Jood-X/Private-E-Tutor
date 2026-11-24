namespace ETutor.Application.Abstractions;

/// <summary>What the provider says about a video when a teacher links it.</summary>
public enum VideoLookupStatus
{
    Found,

    /// <summary>No such video, or it is private (the provider doesn't reveal which).</summary>
    NotFound,

    /// <summary>It exists but its owner turned off embedding, so it can't play inside the app.</summary>
    NotEmbeddable,

    /// <summary>The provider couldn't be reached or answered with an error; try again later.</summary>
    ProviderUnavailable,
}

/// <summary>Metadata of a playable video. Duration and thumbnail are optional: not every lookup reports them.</summary>
public sealed record VideoMetadata(string Title, int? DurationSeconds, string? ThumbnailUrl);

public sealed record VideoLookup(VideoLookupStatus Status, VideoMetadata? Metadata = null)
{
    public static VideoLookup Found(VideoMetadata metadata) => new(VideoLookupStatus.Found, metadata);
}

/// <summary>
/// A video host (YouTube today). The rest of the application only sees a provider name and an opaque asset ID, so
/// a different host with signed playback can be added later without changing the content model (decision D3).
/// </summary>
public interface IVideoProvider
{
    /// <summary>Stable lowercase name stored with each video, e.g. "youtube".</summary>
    string Name { get; }

    /// <summary>Extracts the provider's video ID from what a teacher pasted (a share link or a bare ID).</summary>
    bool TryParseAssetId(string input, out string assetId);

    Task<VideoLookup> LookupAsync(string assetId, CancellationToken cancellationToken);
}
