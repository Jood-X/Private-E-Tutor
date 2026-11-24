using ETutor.Application.Abstractions;
using ETutor.Application.Common;
using ETutor.Application.Common.Results;
using ETutor.Application.Common.Validation;
using ETutor.Application.Courses;
using ETutor.Domain.Content;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ETutor.Application.Videos;

/// <summary>A teacher links a video: a share link (or bare ID), an optional title (defaults to the video's own).</summary>
public sealed record VideoResourceRequest(string Url, string? Title, bool IsRequired);

/// <summary>What the player needs. Returned only to the course's teacher or a student who can see the lesson.</summary>
public sealed record VideoPlaybackDto(Guid ResourceId, string Provider, string AssetId, string Title, int? DurationSeconds);

internal sealed class VideoResourceRequestValidator : AbstractValidator<VideoResourceRequest>
{
    public VideoResourceRequestValidator()
    {
        RuleFor(request => request.Url).NotEmpty().MaximumLength(ContentLimits.UrlMax);
        RuleFor(request => request.Title).MaximumLength(ContentLimits.TitleMax);
    }
}

/// <summary>
/// Adds videos to the caller's lessons. The provider is asked about the video first, so a lesson never points at a
/// video that doesn't exist, is private, or can't be embedded. Each teacher registers a video once
/// (<see cref="VideoAsset"/>); its metadata is refreshed whenever it's linked again.
/// </summary>
public sealed class VideoResourceService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IClock clock,
    IEnumerable<IVideoProvider> providers,
    CourseContentService content,
    IValidator<VideoResourceRequest> validator,
    IDatabaseErrors databaseErrors,
    ILogger<VideoResourceService> logger)
{
    public async Task<Result<LessonDetailDto>> AddVideoAsync(Guid lessonId, VideoResourceRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await validator.ValidateToErrorsAsync(request, cancellationToken) is { } invalid)
        {
            return Result<LessonDetailDto>.Failure(invalid);
        }

        var teacherId = currentUser.RequireUserId();
        var ownsLesson = await db.Lessons.AnyAsync(
            lesson => lesson.Id == lessonId && db.Courses.Any(course => course.Id == lesson.CourseId && course.TeacherId == teacherId),
            cancellationToken);
        if (!ownsLesson)
        {
            return Error.NotFound(ErrorCodes.Course.LessonNotFound, "Lesson not found.");
        }

        var (provider, assetId) = Parse(request.Url.Trim());
        if (provider is null)
        {
            return Error.Validation(ErrorCodes.Video.InvalidUrl, "Paste a YouTube link or video ID.", "url");
        }

        var lookup = await provider.LookupAsync(assetId, cancellationToken);
        VideoLog.Lookup(logger, provider.Name, lookup.Status);
        switch (lookup.Status)
        {
            case VideoLookupStatus.NotFound:
                return Error.BusinessRule(ErrorCodes.Video.NotFound, "The video doesn't exist or is private. Use an unlisted or public video.");
            case VideoLookupStatus.NotEmbeddable:
                return Error.BusinessRule(ErrorCodes.Video.NotEmbeddable, "The video's owner doesn't allow it to play in other apps.");
            case VideoLookupStatus.ProviderUnavailable:
                return Error.Unavailable(ErrorCodes.Video.ProviderUnavailable, "The video service can't be reached. Try again shortly.");
        }

        var metadata = lookup.Metadata!;
        var asset = await UpsertAssetAsync(teacherId, provider.Name, assetId, metadata, cancellationToken);
        var title = string.IsNullOrWhiteSpace(request.Title) ? metadata.Title : request.Title.Trim();
        var maxOrder = await db.LessonResources.Where(item => item.LessonId == lessonId).MaxAsync(item => (int?)item.SortOrder, cancellationToken);
        db.LessonResources.Add(new LessonResource
        {
            Id = Guid.NewGuid(),
            LessonId = lessonId,
            Type = ResourceType.Video,
            Title = Truncate(title, ContentLimits.TitleMax),
            VideoAssetId = asset.Id,
            IsRequired = request.IsRequired,
            SortOrder = ContentRules.NextSortOrder(maxOrder),
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (databaseErrors.IsUniqueViolation(exception))
        {
            // The same teacher registered the same video in a parallel request; asking again will reuse it.
            return Error.Conflict(ErrorCodes.Common.ConcurrencyConflict, "This video was just added elsewhere. Try again.");
        }

        return await content.GetLessonAsync(lessonId, cancellationToken);
    }

    private (IVideoProvider? Provider, string AssetId) Parse(string input)
    {
        foreach (var provider in providers)
        {
            if (provider.TryParseAssetId(input, out var assetId))
            {
                return (provider, assetId);
            }
        }

        return (null, string.Empty);
    }

    private async Task<VideoAsset> UpsertAssetAsync(
        Guid teacherId, string providerName, string assetId, VideoMetadata metadata, CancellationToken cancellationToken)
    {
        var asset = await db.VideoAssets.FirstOrDefaultAsync(
            video => video.OwnerTeacherId == teacherId && video.Provider == providerName && video.ProviderAssetId == assetId,
            cancellationToken);
        if (asset is null)
        {
            asset = new VideoAsset
            {
                Id = Guid.NewGuid(),
                OwnerTeacherId = teacherId,
                Provider = providerName,
                ProviderAssetId = assetId,
                Title = string.Empty,
            };
            db.VideoAssets.Add(asset);
        }

        asset.Title = Truncate(metadata.Title, ContentLimits.TitleMax);
        asset.DurationSeconds = metadata.DurationSeconds;
        asset.ThumbnailUrl = metadata.ThumbnailUrl is { Length: <= ContentLimits.UrlMax } thumbnail ? thumbnail : null;
        asset.Status = VideoStatus.Ready;
        asset.IsEmbeddable = true;
        asset.LastSyncedAtUtc = clock.UtcNow;
        return asset;
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];
}

/// <summary>Video events (EventId range 4000-4099). Provider names and outcomes only.</summary>
internal static partial class VideoLog
{
    [LoggerMessage(EventId = 4000, EventName = "video.lookup", Level = LogLevel.Information,
        Message = "Video lookup at {Provider} returned {Status}")]
    public static partial void Lookup(ILogger logger, string provider, VideoLookupStatus status);
}
