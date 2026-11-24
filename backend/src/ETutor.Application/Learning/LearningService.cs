using ETutor.Application.Abstractions;
using ETutor.Application.Common;
using ETutor.Application.Common.Results;
using ETutor.Application.Courses;
using ETutor.Domain.Content;
using ETutor.Domain.Enrollments;
using Microsoft.EntityFrameworkCore;

namespace ETutor.Application.Learning;

/// <summary>A course for a student: always the public summary; the outline only with an approved enrollment.</summary>
public sealed record StudentCourseDto(
    Guid Id,
    string Title,
    string Description,
    string SubjectName,
    string GradeLevelName,
    Guid TeacherId,
    string TeacherName,
    Guid? EnrollmentId,
    EnrollmentStatus? EnrollmentStatus,
    IReadOnlyList<StudentUnitDto>? Units);

public sealed record StudentUnitDto(Guid Id, string Title, string Description, IReadOnlyList<StudentLessonDto> Lessons);

public sealed record StudentLessonDto(Guid Id, string Title, string Summary, short? EstimatedMinutes);

/// <summary>A document ready to stream to the caller.</summary>
public sealed record FileDownload(Stream Content, string ContentType, string FileName);

/// <summary>
/// What a student may see. The access rule lives in one place (<see cref="VisibleLessons"/>):
/// approved enrollment, and the lesson, its unit and its course all visible (published; an archived course
/// stays readable for students already enrolled).
/// </summary>
public sealed class LearningService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IRequestLanguage language,
    LessonQueries lessonQueries,
    IFileStorage storage)
{
    public async Task<Result<StudentCourseDto>> GetCourseAsync(Guid courseId, CancellationToken cancellationToken)
    {
        var studentId = currentUser.RequireUserId();
        var course = await db.Courses.AsNoTracking()
            .Where(item => item.Id == courseId)
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.Description,
                item.Status,
                SubjectName = item.Subject!.Name,
                GradeName = item.GradeLevel!.Name,
                item.TeacherId,
                TeacherName = item.Teacher!.User!.DisplayName,
                Enrollment = db.Enrollments
                    .Where(e => e.CourseId == item.Id && e.StudentId == studentId
                        && (e.Status == EnrollmentStatus.Pending || e.Status == EnrollmentStatus.Approved))
                    .Select(e => new { e.Id, e.Status })
                    .FirstOrDefault(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var approved = course?.Enrollment?.Status == EnrollmentStatus.Approved;
        var visible = course is not null && (course.Status == ContentStatus.Published || (approved && course.Status == ContentStatus.Archived));
        if (!visible)
        {
            return Error.NotFound(ErrorCodes.Course.NotFound, "Course not found.");
        }

        IReadOnlyList<StudentUnitDto>? units = approved ? await LoadOutlineAsync(courseId, cancellationToken) : null;
        return new StudentCourseDto(
            course!.Id, course.Title, course.Description, course.SubjectName.Resolve(language.Code), course.GradeName.Resolve(language.Code),
            course.TeacherId, course.TeacherName, course.Enrollment?.Id, course.Enrollment?.Status, units);
    }

    public async Task<Result<LessonDetailDto>> GetLessonAsync(Guid lessonId, CancellationToken cancellationToken) =>
        await lessonQueries.GetDetailAsync(VisibleLessons(), lessonId, cancellationToken) is { } lesson
            ? lesson
            : Error.NotFound(ErrorCodes.Course.LessonNotFound, "Lesson not found.");

    /// <summary>Opens a document for its course's teacher or for a student who can see the lesson.</summary>
    public async Task<Result<FileDownload>> OpenDocumentAsync(Guid resourceId, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var visibleToStudent = VisibleLessons();
        var file = await db.LessonResources.AsNoTracking()
            .Where(resource => resource.Id == resourceId && resource.Type == ResourceType.Document)
            .Where(resource => db.Courses.Any(course => course.Id == resource.Lesson!.CourseId && course.TeacherId == userId)
                || visibleToStudent.Any(lesson => lesson.Id == resource.LessonId))
            .Select(resource => new { resource.File!.StorageKey, resource.File.ContentType, resource.File.OriginalFileName })
            .FirstOrDefaultAsync(cancellationToken);
        if (file is null)
        {
            return Error.NotFound(ErrorCodes.Course.ResourceNotFound, "Resource not found.");
        }

        var stream = await storage.OpenReadAsync(file.StorageKey, cancellationToken);
        return new FileDownload(stream, file.ContentType, file.OriginalFileName);
    }

    private IQueryable<Lesson> VisibleLessons()
    {
        var studentId = currentUser.RequireUserId();
        return db.Lessons.Where(lesson =>
            lesson.Status == ContentStatus.Published
            && lesson.Unit!.Status == ContentStatus.Published
            && lesson.Unit.Course!.Status != ContentStatus.Draft
            && db.Enrollments.Any(e => e.CourseId == lesson.CourseId && e.StudentId == studentId && e.Status == EnrollmentStatus.Approved));
    }

    private async Task<IReadOnlyList<StudentUnitDto>> LoadOutlineAsync(Guid courseId, CancellationToken cancellationToken)
    {
        var units = await db.Units.AsNoTracking()
            .Where(unit => unit.CourseId == courseId && unit.Status == ContentStatus.Published)
            .OrderBy(unit => unit.SortOrder)
            .Select(unit => new { unit.Id, unit.Title, unit.Description })
            .ToListAsync(cancellationToken);
        var lessons = await db.Lessons.AsNoTracking()
            .Where(lesson => lesson.CourseId == courseId && lesson.Status == ContentStatus.Published)
            .OrderBy(lesson => lesson.SortOrder)
            .Select(lesson => new { lesson.UnitId, lesson.Id, lesson.Title, lesson.Summary, lesson.EstimatedMinutes })
            .ToListAsync(cancellationToken);
        var byUnit = lessons.ToLookup(lesson => lesson.UnitId);
        return units.ConvertAll(unit => new StudentUnitDto(
            unit.Id, unit.Title, unit.Description,
            byUnit[unit.Id].Select(lesson => new StudentLessonDto(lesson.Id, lesson.Title, lesson.Summary, lesson.EstimatedMinutes)).ToList()));
    }
}
