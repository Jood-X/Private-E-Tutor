using ETutor.Application.Abstractions;
using ETutor.Application.Common;
using ETutor.Application.Common.Results;
using ETutor.Application.Common.Validation;
using ETutor.Domain.Content;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ETutor.Application.Courses;

/// <summary>
/// Units and lessons of the caller's own courses. Mutations return the refreshed course outline (one round
/// trip for the editor). Ownership is part of every query: other teachers' items are 404.
/// </summary>
public sealed class CourseContentService(
    IAppDbContext db,
    ICurrentUser currentUser,
    TeacherCourseService courses,
    LessonQueries lessonQueries,
    IValidator<UnitRequest> unitValidator,
    IValidator<LessonRequest> lessonValidator)
{
    // ---- Units ----

    public async Task<Result<TeacherCourseDto>> CreateUnitAsync(Guid courseId, UnitRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await unitValidator.ValidateToErrorsAsync(request, cancellationToken) is { } invalid)
        {
            return Result<TeacherCourseDto>.Failure(invalid);
        }

        var teacherId = currentUser.RequireUserId();
        if (!await db.Courses.AnyAsync(course => course.Id == courseId && course.TeacherId == teacherId, cancellationToken))
        {
            return Error.NotFound(ErrorCodes.Course.NotFound, "Course not found.");
        }

        var maxOrder = await db.Units.Where(unit => unit.CourseId == courseId).MaxAsync(unit => (int?)unit.SortOrder, cancellationToken);
        db.Units.Add(new Unit
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            SortOrder = ContentRules.NextSortOrder(maxOrder),
        });
        await db.SaveChangesAsync(cancellationToken);
        return await courses.GetAsync(courseId, cancellationToken);
    }

    public async Task<Result<TeacherCourseDto>> UpdateUnitAsync(Guid unitId, UnitRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await unitValidator.ValidateToErrorsAsync(request, cancellationToken) is { } invalid)
        {
            return Result<TeacherCourseDto>.Failure(invalid);
        }

        var unit = await OwnedUnits().FirstOrDefaultAsync(item => item.Id == unitId, cancellationToken);
        if (unit is null)
        {
            return UnitNotFound();
        }

        unit.Title = request.Title.Trim();
        unit.Description = request.Description.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return await courses.GetAsync(unit.CourseId, cancellationToken);
    }

    public async Task<Result<TeacherCourseDto>> ChangeUnitStatusAsync(Guid unitId, ContentStatus status, CancellationToken cancellationToken)
    {
        var unit = await OwnedUnits().FirstOrDefaultAsync(item => item.Id == unitId, cancellationToken);
        if (unit is null)
        {
            return UnitNotFound();
        }

        unit.Status = status;
        await db.SaveChangesAsync(cancellationToken);
        return await courses.GetAsync(unit.CourseId, cancellationToken);
    }

    /// <summary>Deletes a draft unit and its lessons. Published units are archived instead (student history).</summary>
    public async Task<Result<TeacherCourseDto>> DeleteUnitAsync(Guid unitId, CancellationToken cancellationToken)
    {
        var unit = await OwnedUnits().FirstOrDefaultAsync(item => item.Id == unitId, cancellationToken);
        if (unit is null)
        {
            return UnitNotFound();
        }

        var publishedLesson = await db.Lessons.AnyAsync(lesson => lesson.UnitId == unitId && lesson.Status != ContentStatus.Draft, cancellationToken);
        if (!ContentRules.CanDelete(unit.Status) || publishedLesson)
        {
            return OnlyDraftCanBeDeleted();
        }

        db.Units.Remove(unit);
        await db.SaveChangesAsync(cancellationToken);
        return await courses.GetAsync(unit.CourseId, cancellationToken);
    }

    public async Task<Result<TeacherCourseDto>> MoveUnitAsync(Guid unitId, int position, CancellationToken cancellationToken)
    {
        var unit = await OwnedUnits().AsNoTracking().FirstOrDefaultAsync(item => item.Id == unitId, cancellationToken);
        if (unit is null)
        {
            return UnitNotFound();
        }

        var siblings = await db.Units.Where(item => item.CourseId == unit.CourseId).OrderBy(item => item.SortOrder).ToListAsync(cancellationToken);
        Reorder(siblings, unitId, position, item => item.Id, (item, order) => item.SortOrder = order);
        await db.SaveChangesAsync(cancellationToken);
        return await courses.GetAsync(unit.CourseId, cancellationToken);
    }

    // ---- Lessons ----

    public async Task<Result<TeacherCourseDto>> CreateLessonAsync(Guid unitId, LessonRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await lessonValidator.ValidateToErrorsAsync(request, cancellationToken) is { } invalid)
        {
            return Result<TeacherCourseDto>.Failure(invalid);
        }

        var unit = await OwnedUnits().AsNoTracking().FirstOrDefaultAsync(item => item.Id == unitId, cancellationToken);
        if (unit is null)
        {
            return UnitNotFound();
        }

        var maxOrder = await db.Lessons.Where(lesson => lesson.UnitId == unitId).MaxAsync(lesson => (int?)lesson.SortOrder, cancellationToken);
        db.Lessons.Add(new Lesson
        {
            Id = Guid.NewGuid(),
            UnitId = unitId,
            CourseId = unit.CourseId,
            Title = request.Title.Trim(),
            Summary = request.Summary.Trim(),
            Notes = request.Notes,
            EstimatedMinutes = request.EstimatedMinutes,
            SortOrder = ContentRules.NextSortOrder(maxOrder),
        });
        await db.SaveChangesAsync(cancellationToken);
        return await courses.GetAsync(unit.CourseId, cancellationToken);
    }

    public async Task<Result<LessonDetailDto>> GetLessonAsync(Guid lessonId, CancellationToken cancellationToken) =>
        await lessonQueries.GetDetailAsync(OwnedLessons(), lessonId, cancellationToken) is { } lesson
            ? lesson
            : LessonNotFound();

    public async Task<Result<LessonDetailDto>> UpdateLessonAsync(Guid lessonId, LessonRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await lessonValidator.ValidateToErrorsAsync(request, cancellationToken) is { } invalid)
        {
            return Result<LessonDetailDto>.Failure(invalid);
        }

        var lesson = await OwnedLessons().FirstOrDefaultAsync(item => item.Id == lessonId, cancellationToken);
        if (lesson is null)
        {
            return LessonNotFound();
        }

        lesson.Title = request.Title.Trim();
        lesson.Summary = request.Summary.Trim();
        lesson.Notes = request.Notes;
        lesson.EstimatedMinutes = request.EstimatedMinutes;
        await db.SaveChangesAsync(cancellationToken);
        return await GetLessonAsync(lessonId, cancellationToken);
    }

    public async Task<Result<TeacherCourseDto>> ChangeLessonStatusAsync(Guid lessonId, ContentStatus status, CancellationToken cancellationToken)
    {
        var lesson = await OwnedLessons().FirstOrDefaultAsync(item => item.Id == lessonId, cancellationToken);
        if (lesson is null)
        {
            return LessonNotFound();
        }

        lesson.Status = status;
        await db.SaveChangesAsync(cancellationToken);
        return await courses.GetAsync(lesson.CourseId, cancellationToken);
    }

    public async Task<Result<TeacherCourseDto>> DeleteLessonAsync(Guid lessonId, CancellationToken cancellationToken)
    {
        var lesson = await OwnedLessons().FirstOrDefaultAsync(item => item.Id == lessonId, cancellationToken);
        if (lesson is null)
        {
            return LessonNotFound();
        }

        if (!ContentRules.CanDelete(lesson.Status))
        {
            return OnlyDraftCanBeDeleted();
        }

        db.Lessons.Remove(lesson);
        await db.SaveChangesAsync(cancellationToken);
        return await courses.GetAsync(lesson.CourseId, cancellationToken);
    }

    public async Task<Result<TeacherCourseDto>> MoveLessonAsync(Guid lessonId, int position, CancellationToken cancellationToken)
    {
        var lesson = await OwnedLessons().AsNoTracking().FirstOrDefaultAsync(item => item.Id == lessonId, cancellationToken);
        if (lesson is null)
        {
            return LessonNotFound();
        }

        var siblings = await db.Lessons.Where(item => item.UnitId == lesson.UnitId).OrderBy(item => item.SortOrder).ToListAsync(cancellationToken);
        Reorder(siblings, lessonId, position, item => item.Id, (item, order) => item.SortOrder = order);
        await db.SaveChangesAsync(cancellationToken);
        return await courses.GetAsync(lesson.CourseId, cancellationToken);
    }

    private IQueryable<Unit> OwnedUnits()
    {
        var teacherId = currentUser.RequireUserId();
        return db.Units.Where(unit => unit.Course!.TeacherId == teacherId);
    }

    private IQueryable<Lesson> OwnedLessons()
    {
        var teacherId = currentUser.RequireUserId();
        return db.Lessons.Where(lesson => db.Courses.Any(course => course.Id == lesson.CourseId && course.TeacherId == teacherId));
    }

    /// <summary>Moves one item to <paramref name="position"/> (0-based, clamped) and renumbers all siblings.</summary>
    private static void Reorder<T>(List<T> siblings, Guid id, int position, Func<T, Guid> idOf, Action<T, int> setOrder)
    {
        var item = siblings.First(sibling => idOf(sibling) == id);
        siblings.Remove(item);
        siblings.Insert(Math.Clamp(position, 0, siblings.Count), item);
        for (var index = 0; index < siblings.Count; index++)
        {
            setOrder(siblings[index], (index + 1) * ContentRules.SortOrderStep);
        }
    }

    private static Error UnitNotFound() => Error.NotFound(ErrorCodes.Course.UnitNotFound, "Unit not found.");

    private static Error LessonNotFound() => Error.NotFound(ErrorCodes.Course.LessonNotFound, "Lesson not found.");

    private static Error OnlyDraftCanBeDeleted() => Error.BusinessRule(
        ErrorCodes.Course.OnlyDraftCanBeDeleted, "Only drafts can be deleted. Archive published content instead.");
}
