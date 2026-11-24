using System.Security.Cryptography;
using ETutor.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace ETutor.Infrastructure.Files;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Folder for uploaded files. Relative paths resolve against the app's content root.</summary>
    public string RootPath { get; init; } = "App_Data/files";
}

/// <summary>
/// Development storage on the local disk, outside any web-served folder. Keys are random and
/// generated here; they are checked to stay inside <see cref="FileStorageOptions.RootPath"/>.
/// Replace with blob storage for production (same interface).
/// </summary>
internal sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IOptions<FileStorageOptions> options, Microsoft.Extensions.Hosting.IHostEnvironment environment)
    {
        var configured = options.Value.RootPath;
        _root = Path.GetFullPath(Path.IsPathRooted(configured) ? configured : Path.Combine(environment.ContentRootPath, configured));
        Directory.CreateDirectory(_root);
    }

    public string ProviderName => "local";

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        var safeExtension = extension is ".pdf" ? extension : string.Empty;
        // Two-level fan-out keeps directories small: ab/cd/abcdef….pdf
        var name = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant() + safeExtension;
        var key = $"{name[..2]}/{name[2..4]}/{name}";
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(file, cancellationToken);
        return key;
    }

    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken) =>
        Task.FromResult<Stream>(new FileStream(Resolve(key), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true));

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        var path = Resolve(key);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    /// <summary>Maps a key to a path and refuses anything that would escape the storage root.</summary>
    private string Resolve(string key)
    {
        var path = Path.GetFullPath(Path.Combine(_root, key));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Storage key resolves outside the storage root.");
        }

        return path;
    }
}
