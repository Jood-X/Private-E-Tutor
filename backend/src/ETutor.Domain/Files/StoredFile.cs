namespace ETutor.Domain.Files;

public enum FilePurpose : byte
{
    LessonDocument = 1,
}

/// <summary>
/// Metadata of an uploaded file. The bytes live in file storage under a random <see cref="StorageKey"/>;
/// the original name is kept only for display and download, never used as a path.
/// </summary>
public sealed class StoredFile
{
    public const int Sha256Length = 32;

    public Guid Id { get; set; }

    public Guid OwnerUserId { get; set; }

    public FilePurpose Purpose { get; set; }

    public required string StorageProvider { get; set; }

    public required string StorageKey { get; set; }

    public required string OriginalFileName { get; set; }

    public required string ContentType { get; set; }

    public long SizeBytes { get; set; }

    public required byte[] Sha256 { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
