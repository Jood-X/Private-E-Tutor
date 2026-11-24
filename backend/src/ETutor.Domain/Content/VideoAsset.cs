using ETutor.Domain.Common;
using ETutor.Domain.Users;

namespace ETutor.Domain.Content;

/// <summary>Whether a registered video can currently be played.</summary>
public enum VideoStatus : byte
{
    Ready = 1,

    /// <summary>The provider no longer serves it (deleted, made private, or embedding turned off).</summary>
    Unavailable = 2,
}

/// <summary>
/// A video a teacher registered from a hosting provider. Provider-independent: <see cref="Provider"/> names the
/// host ("youtube") and <see cref="ProviderAssetId"/> is that host's ID. One row per teacher and video, reused by
/// every lesson resource that plays it.
/// </summary>
public sealed class VideoAsset : IAuditable
{
    public Guid Id { get; set; }

    public Guid OwnerTeacherId { get; set; }

    public required string Provider { get; set; }

    public required string ProviderAssetId { get; set; }

    public required string Title { get; set; }

    /// <summary>Null when the provider doesn't report it (yet).</summary>
    public int? DurationSeconds { get; set; }

    public string? ThumbnailUrl { get; set; }

    public VideoStatus Status { get; set; } = VideoStatus.Ready;

    public bool IsEmbeddable { get; set; } = true;

    public DateTime LastSyncedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public TeacherProfile? OwnerTeacher { get; set; }
}
