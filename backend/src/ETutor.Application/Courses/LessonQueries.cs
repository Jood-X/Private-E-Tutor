using ETutor.Application.Abstractions;
using ETutor.Domain.Content;
using Microsoft.EntityFrameworkCore;

namespace ETutor.Application.Courses;

/// <summary>Projects a lesson with its resources. Callers pass a query already filtered by access rules.</summary>
public sealed class LessonQueries(IAppDbContext db)
{
    public async Task<LessonDetailDto?> GetDetailAsync(IQueryable<Lesson> accessibleLessons, Guid lessonId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accessibleLessons);
        var lesson = await accessibleLessons.AsNoTracking()
            .Where(item => item.Id == lessonId)
            .Select(item => new { item.Id, item.UnitId, item.CourseId, item.Title, item.Summary, item.Notes, item.EstimatedMinutes, item.Status })
            .FirstOrDefaultAsync(cancellationToken);
        if (lesson is null)
        {
            return null;
        }

        var resources = await db.LessonResources.AsNoTracking()
            .Where(resource => resource.LessonId == lessonId)
            .OrderBy(resource => resource.SortOrder)
            .Select(resource => new ResourceDto(
                resource.Id,
                resource.Type,
                resource.Title,
                resource.IsRequired,
                resource.ExternalUrl,
                resource.File == null ? null : resource.File.OriginalFileName,
                resource.File == null ? null : resource.File.SizeBytes,
                resource.VideoAsset == null
                    ? null
                    : new ResourceVideoDto(resource.VideoAsset.DurationSeconds, resource.VideoAsset.ThumbnailUrl, resource.VideoAsset.Status)))
            .ToListAsync(cancellationToken);

        return new LessonDetailDto(
            lesson.Id, lesson.UnitId, lesson.CourseId, lesson.Title, lesson.Summary, lesson.Notes,
            lesson.EstimatedMinutes, lesson.Status, resources);
    }
}
