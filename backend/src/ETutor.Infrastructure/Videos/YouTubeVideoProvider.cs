using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using ETutor.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ETutor.Infrastructure.Videos;

public sealed class YouTubeOptions
{
    public const string SectionName = "YouTube";

    /// <summary>
    /// YouTube Data API v3 key (server-side only; set with user-secrets or the <c>YouTube__ApiKey</c> environment
    /// variable, never in appsettings). Without it, lookups fall back to oEmbed: no duration, and private videos and
    /// videos with embedding turned off can't be told apart.
    /// </summary>
    public string? ApiKey { get; set; }

    public Uri DataApiBaseUrl { get; set; } = new("https://www.googleapis.com/youtube/v3/");

    public Uri OEmbedUrl { get; set; } = new("https://www.youtube.com/oembed");

    public int TimeoutSeconds { get; set; } = 10;
}

/// <summary>
/// YouTube as a video provider (decision D3: teachers link unlisted or public videos from their own channel).
/// Lookups use the Data API when a key is configured, otherwise the keyless oEmbed endpoint.
/// </summary>
public sealed partial class YouTubeVideoProvider(HttpClient http, IOptions<YouTubeOptions> options, ILogger<YouTubeVideoProvider> logger)
    : IVideoProvider
{
    public const string ProviderName = "youtube";

    private static readonly string[] Hosts = ["youtube.com", "www.youtube.com", "m.youtube.com", "music.youtube.com", "youtube-nocookie.com", "www.youtube-nocookie.com"];
    private static readonly string[] IdPathPrefixes = ["/embed/", "/shorts/", "/live/", "/v/"];

    public string Name => ProviderName;

    public bool TryParseAssetId(string input, out string assetId) => TryParse(input, out assetId);

    /// <summary>
    /// Accepts a bare 11-character ID or a link from youtube.com (watch, embed, shorts, live), m./music. subdomains,
    /// youtube-nocookie.com or youtu.be; the scheme may be omitted. Anything else, including other hosts, is refused.
    /// </summary>
    public static bool TryParse(string input, out string assetId)
    {
        assetId = string.Empty;
        var text = (input ?? string.Empty).Trim();
        if (VideoId().IsMatch(text))
        {
            assetId = text;
            return true;
        }

        if (!text.Contains("://", StringComparison.Ordinal))
        {
            text = "https://" + text;
        }

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
        {
            return false;
        }

        var host = uri.Host.ToLowerInvariant();
        string? candidate = null;
        if (host is "youtu.be" or "www.youtu.be")
        {
            candidate = uri.AbsolutePath.Trim('/').Split('/')[0];
        }
        else if (Hosts.Contains(host))
        {
            if (uri.AbsolutePath.Equals("/watch", StringComparison.OrdinalIgnoreCase))
            {
                candidate = QueryValue(uri.Query, "v");
            }
            else if (IdPathPrefixes.FirstOrDefault(prefix => uri.AbsolutePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) is { } prefix)
            {
                candidate = uri.AbsolutePath[prefix.Length..].Split('/')[0];
            }
        }

        if (candidate is null || !VideoId().IsMatch(candidate))
        {
            return false;
        }

        assetId = candidate;
        return true;
    }

    public async Task<VideoLookup> LookupAsync(string assetId, CancellationToken cancellationToken)
    {
        if (!VideoId().IsMatch(assetId))
        {
            return new VideoLookup(VideoLookupStatus.NotFound);
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));
            return string.IsNullOrWhiteSpace(options.Value.ApiKey)
                ? await LookupWithOEmbedAsync(assetId, timeout.Token)
                : await LookupWithDataApiAsync(assetId, options.Value.ApiKey, timeout.Token);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException
            || (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            YouTubeLog.Unreachable(logger, exception.GetType().Name);
            return new VideoLookup(VideoLookupStatus.ProviderUnavailable);
        }
    }

    private async Task<VideoLookup> LookupWithDataApiAsync(string assetId, string apiKey, CancellationToken cancellationToken)
    {
        var uri = new Uri(options.Value.DataApiBaseUrl, $"videos?part=snippet,contentDetails,status&id={assetId}");
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Add("X-Goog-Api-Key", apiKey); // header, not query string: keeps the key out of request logs
        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            YouTubeLog.ErrorStatus(logger, (int)response.StatusCode);
            return new VideoLookup(VideoLookupStatus.ProviderUnavailable);
        }

        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);
        return ReadDataApiResponse(json.RootElement);
    }

    /// <summary>Maps a Data API <c>videos.list</c> response (public for unit tests).</summary>
    public static VideoLookup ReadDataApiResponse(JsonElement root)
    {
        if (!root.TryGetProperty("items", out var items) || items.GetArrayLength() == 0)
        {
            return new VideoLookup(VideoLookupStatus.NotFound);
        }

        var item = items[0];
        var status = item.GetProperty("status");
        if (status.TryGetProperty("privacyStatus", out var privacy) && privacy.GetString() == "private")
        {
            return new VideoLookup(VideoLookupStatus.NotFound);
        }

        if (status.TryGetProperty("embeddable", out var embeddable) && embeddable.ValueKind == JsonValueKind.False)
        {
            return new VideoLookup(VideoLookupStatus.NotEmbeddable);
        }

        var snippet = item.GetProperty("snippet");
        var title = snippet.TryGetProperty("title", out var titleElement) ? titleElement.GetString() : null;
        string? thumbnail = null;
        if (snippet.TryGetProperty("thumbnails", out var thumbnails))
        {
            foreach (var size in (string[])["medium", "high", "default"])
            {
                if (thumbnails.TryGetProperty(size, out var image) && image.TryGetProperty("url", out var url))
                {
                    thumbnail = url.GetString();
                    break;
                }
            }
        }

        int? duration = null;
        if (item.TryGetProperty("contentDetails", out var details) && details.TryGetProperty("duration", out var isoDuration))
        {
            duration = ParseDuration(isoDuration.GetString());
        }

        return VideoLookup.Found(new VideoMetadata(string.IsNullOrWhiteSpace(title) ? "Video" : title, duration, thumbnail));
    }

    /// <summary>ISO 8601 duration ("PT1H2M3S") to seconds; null for live streams ("P0D") or anything unreadable.</summary>
    public static int? ParseDuration(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso))
        {
            return null;
        }

        try
        {
            var seconds = (int)XmlConvert.ToTimeSpan(iso).TotalSeconds;
            return seconds > 0 ? seconds : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private async Task<VideoLookup> LookupWithOEmbedAsync(string assetId, CancellationToken cancellationToken)
    {
        var watchUrl = Uri.EscapeDataString($"https://www.youtube.com/watch?v={assetId}");
        using var response = await http.GetAsync(new Uri($"{options.Value.OEmbedUrl}?url={watchUrl}&format=json"), cancellationToken);
        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
                await using (var body = await response.Content.ReadAsStreamAsync(cancellationToken))
                {
                    using var json = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);
                    var root = json.RootElement;
                    var title = root.TryGetProperty("title", out var titleElement) ? titleElement.GetString() : null;
                    var thumbnail = root.TryGetProperty("thumbnail_url", out var image) ? image.GetString() : null;
                    return VideoLookup.Found(new VideoMetadata(string.IsNullOrWhiteSpace(title) ? "Video" : title, null, thumbnail));
                }

            case HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden:
                // oEmbed refuses videos whose embedding is off (and, without a key, private ones look the same).
                return new VideoLookup(VideoLookupStatus.NotEmbeddable);
            case HttpStatusCode.NotFound or HttpStatusCode.BadRequest:
                return new VideoLookup(VideoLookupStatus.NotFound);
            default:
                YouTubeLog.ErrorStatus(logger, (int)response.StatusCode);
                return new VideoLookup(VideoLookupStatus.ProviderUnavailable);
        }
    }

    private static string? QueryValue(string query, string name)
    {
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=', StringComparison.Ordinal);
            if (separator > 0 && pair[..separator] == name)
            {
                return Uri.UnescapeDataString(pair[(separator + 1)..]);
            }
        }

        return null;
    }

    [GeneratedRegex("^[A-Za-z0-9_-]{11}$")]
    private static partial Regex VideoId();
}

/// <summary>YouTube provider events (EventId range 4010-4019). Never logs the API key or request URLs.</summary>
internal static partial class YouTubeLog
{
    [LoggerMessage(EventId = 4010, EventName = "youtube.error_status", Level = LogLevel.Warning,
        Message = "YouTube answered a video lookup with HTTP {StatusCode}")]
    public static partial void ErrorStatus(ILogger logger, int statusCode);

    [LoggerMessage(EventId = 4011, EventName = "youtube.unreachable", Level = LogLevel.Warning,
        Message = "YouTube video lookup failed ({ExceptionType})")]
    public static partial void Unreachable(ILogger logger, string exceptionType);
}
