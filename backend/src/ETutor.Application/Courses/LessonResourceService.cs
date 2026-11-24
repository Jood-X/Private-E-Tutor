using System.Security.Cryptography;
using ETutor.Application.Abstractions;
using ETutor.Application.Common;
using ETutor.Application.Common.Results;
using ETutor.Application.Common.Validation;
using ETutor.Domain.Content;
using ETutor.Domain.Files;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ETutor.Application.Courses;

/// <summary>
/// Links and PDF documents attached to the caller's own lessons. Documents are validated by size and by
/// their actual bytes (PDF signature), not by the file name or the client-supplied content type.
/// </summary>
public sealed class LessonResourceService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IFileStorage storage,
    IClock clock,
    CourseContentService content,
    IValidator<LinkResourceRequest> linkValidator)
{
    public const long MaxDocumentBytes = 20 * 1024 * 1024;
    public const string PdfContentType = "application/pdf";
    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();

    public async Task<Result<LessonDetailDto>> AddLinkAsync(Guid lessonId, LinkResourceRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await linkValidator.ValidateToErrorsAsync(request, cancellationToken) is { } invalid)
        {
            return Result<LessonDetailDto>.Failure(invalid);
        }

        if (!await OwnsLessonAsync(lessonId, cancellationToken))
        {
            return LessonNotFound();
        }

        db.LessonResources.Add(new LessonResource
        {
            Id = Guid.NewGuid(),
            LessonId = lessonId,
            Type = ResourceType.Link,
            Title = request.Title.Trim(),
            ExternalUrl = request.Url.Trim(),
            IsRequired = request.IsRequired,
            SortOrder = await NextOrderAsync(lessonId, cancellationToken),
        });
        await db.SaveChangesAsync(cancellationToken);
        return await content.GetLessonAsync(lessonId, cancellationToken);
    }

    public async Task<Result<LessonDetailDto>> AddDocumentAsync(Guid lessonId, DocumentUpload upload, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(upload);
        if (string.IsNullOrWhiteSpace(upload.Title) || upload.Title.Length > ContentLimits.TitleMax)
        {
            return Error.Validation(ErrorCodes.Common.ValidationFailed, "A title of up to 200 characters is required.", "title");
        }

        if (upload.Length is <= 0 or > MaxDocumentBytes)
        {
            return Error.Validation(ErrorCodes.Course.DocumentTooLarge, "Documents must be between 1 byte and 20 MB.", "file");
        }

        if (!await OwnsLessonAsync(lessonId, cancellationToken))
        {
            return LessonNotFound();
        }

        // Buffer (bounded by the size limit) so the signature and hash come from the exact bytes stored.
        using var buffer = new MemoryStream();
        await upload.Content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        if (bytes.Length > MaxDocumentBytes || !bytes.AsSpan().StartsWith(PdfSignature))
        {
            return Error.Validation(ErrorCodes.Course.InvalidDocument, "Only PDF documents are supported.", "file");
        }

        var key = await storage.SaveAsync(new MemoryStream(bytes, writable: false), ".pdf", cancellationToken);
        try
        {
            var file = new StoredFile
            {
                Id = Guid.NewGuid(),
                OwnerUserId = currentUser.RequireUserId(),
                Purpose = FilePurpose.LessonDocument,
                StorageProvider = storage.ProviderName,
                StorageKey = key,
                OriginalFileName = SafeFileName(upload.FileName),
                ContentType = PdfContentType,
                SizeBytes = bytes.Length,
                Sha256 = SHA256.HashData(bytes),
                CreatedAtUtc = clock.UtcNow,
            };
            db.StoredFiles.Add(file);
            db.LessonResources.Add(new LessonResource
            {
                Id = Guid.NewGuid(),
                LessonId = lessonId,
                Type = ResourceType.Document,
                Title = upload.Title.Trim(),
                FileId = file.Id,
                IsRequired = upload.IsRequired,
                SortOrder = await NextOrderAsync(lessonId, cancellationToken),
            });
            await db.SaveChangesAsync(cancellationToken); // one transaction: file row + resource row
        }
        catch
        {
            // Don't leave orphaned bytes when the database write fails.
            await storage.DeleteAsync(key, CancellationToken.None);
            throw;
        }

        return await content.GetLessonAsync(lessonId, cancellationToken);
    }

    public async Task<Result<LessonDetailDto>> DeleteAsync(Guid resourceId, CancellationToken cancellationToken)
    {
        var teacherId = currentUser.RequireUserId();
        var resource = await db.LessonResources
            .Include(item => item.File)
            .Where(item => db.Courses.Any(course => course.Id == item.Lesson!.CourseId && course.TeacherId == teacherId))
            .FirstOrDefaultAsync(item => item.Id == resourceId, cancellationToken);
        if (resource is null)
        {
            return Error.NotFound(ErrorCodes.Course.ResourceNotFound, "Resource not found.");
        }

        var storageKey = resource.File?.StorageKey;
        db.LessonResources.Remove(resource);
        if (resource.File is not null)
        {
            db.StoredFiles.Remove(resource.File);
        }

        await db.SaveChangesAsync(cancellationToken);
        if (storageKey is not null)
        {
            await storage.DeleteAsync(storageKey, cancellationToken);
        }

        return await content.GetLessonAsync(resource.LessonId, cancellationToken);
    }

    private Task<bool> OwnsLessonAsync(Guid lessonId, CancellationToken cancellationToken)
    {
        var teacherId = currentUser.RequireUserId();
        return db.Lessons.AnyAsync(
            lesson => lesson.Id == lessonId && db.Courses.Any(course => course.Id == lesson.CourseId && course.TeacherId == teacherId),
            cancellationToken);
    }

    private async Task<int> NextOrderAsync(Guid lessonId, CancellationToken cancellationToken) => ContentRules.NextSortOrder(
        await db.LessonResources.Where(item => item.LessonId == lessonId).MaxAsync(item => (int?)item.SortOrder, cancellationToken));

    /// <summary>Keeps only the name part (no directories) and a bounded length; used for display only.</summary>
    private static string SafeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(name))
        {
            name = "document.pdf";
        }

        return name.Length <= 255 ? name : name[^255..];
    }

    private static Error LessonNotFound() => Error.NotFound(ErrorCodes.Course.LessonNotFound, "Lesson not found.");
}
