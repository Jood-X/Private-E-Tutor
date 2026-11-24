using ETutor.Application.Abstractions;
using ETutor.Application.Common;
using ETutor.Application.Common.Results;
using ETutor.Application.Common.Validation;
using ETutor.Domain.Content;
using ETutor.Domain.Enrollments;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ETutor.Application.Courses;

/// <summary>
/// A teacher's own courses: create, edit, change status, read the full outline. Every query filters by
/// the caller's user ID, so another teacher's course is indistinguishable from a missing one (404).
/// </summary>
public sealed class TeacherCourseService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IRequestLanguage language,
    IClock clock,
    IValidator<CreateCourseRequest> createValidator,
    IValidator<UpdateCourseRequest> updateValidator)
{
    public async Task<Result<TeacherCourseDto>> CreateAsync(CreateCourseRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await createValidator.ValidateToErrorsAsync(request, cancellationToken) is { } invalid)
        {
            return Result<TeacherCourseDto>.Failure(invalid);
        }

        if (!await IsValidCatalogPlacementAsync(request, cancellationToken))
        {
            return Error.Validation(ErrorCodes.Course.InvalidCatalogSelection,
                "The grade, subject and academic year must belong to the selected curriculum.", "gradeLevelId");
        }

        var course = new Course
        {
            Id = Guid.NewGuid(),
            TeacherId = currentUser.RequireUserId(),
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            ContentLanguage = request.ContentLanguage.ToLowerInvariant(),
            CurriculumId = request.CurriculumId,
            GradeLevelId = request.GradeLevelId,
            SubjectId = request.SubjectId,
            AcademicYearId = request.AcademicYearId,
        };
        db.Courses.Add(course);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(course.Id, cancellationToken);
    }

    public async Task<Result<TeacherCourseDto>> UpdateAsync(Guid courseId, UpdateCourseRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await updateValidator.ValidateToErrorsAsync(request, cancellationToken) is { } invalid)
        {
            return Result<TeacherCourseDto>.Failure(invalid);
        }

        var course = await OwnedCourses().FirstOrDefaultAsync(item => item.Id == courseId, cancellationToken);
        if (course is null)
        {
            return CourseNotFound();
        }

        course.Title = request.Title.Trim();
        course.Description = request.Description.Trim();
        course.ContentLanguage = request.ContentLanguage.ToLowerInvariant();
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(courseId, cancellationToken);
    }

    public async Task<Result<TeacherCourseDto>> ChangeStatusAsync(Guid courseId, ContentStatus status, CancellationToken cancellationToken)
    {
        var course = await OwnedCourses().FirstOrDefaultAsync(item => item.Id == courseId, cancellationToken);
        if (course is null)
        {
            return CourseNotFound();
        }

        var hasPublishedLesson = await db.Lessons.AnyAsync(
            lesson => lesson.CourseId == courseId && lesson.Status == ContentStatus.Published
                && lesson.Unit!.Status == ContentStatus.Published, cancellationToken);
        var hasStudents = await db.Enrollments.AnyAsync(
            enrollment => enrollment.CourseId == courseId
                && (enrollment.Status == EnrollmentStatus.Pending || enrollment.Status == EnrollmentStatus.Approved),
            cancellationToken);

        var problem = ContentRules.CheckCourseStatusChange(course.Status, status, hasPublishedLesson, hasStudents);
        if (problem is not null)
        {
            return problem switch
            {
                ContentRules.CourseStatusProblem.NoPublishedLesson => Error.BusinessRule(ErrorCodes.Course.NoPublishedLesson,
                    "Publish at least one lesson in a published unit before publishing the course."),
                ContentRules.CourseStatusProblem.HasStudents => Error.BusinessRule(ErrorCodes.Course.HasStudents,
                    "This course has students. Archive it instead of moving it back to draft."),
                _ => Error.BusinessRule(ErrorCodes.Course.StatusChangeNotAllowed, "This status change is not allowed."),
            };
        }

        course.Status = status;
        if (status == ContentStatus.Published)
        {
            course.PublishedAtUtc ??= clock.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(courseId, cancellationToken);
    }

    public async Task<IReadOnlyList<TeacherCourseSummaryDto>> ListMineAsync(CancellationToken cancellationToken)
    {
        var rows = await OwnedCourses().AsNoTracking()
            .OrderByDescending(course => course.UpdatedAtUtc)
            .Select(course => new
            {
                course.Id,
                course.Title,
                course.Status,
                SubjectName = course.Subject!.Name,
                GradeName = course.GradeLevel!.Name,
                CurriculumName = course.Curriculum!.Name,
                UnitCount = course.Units.Count(unit => unit.Status != ContentStatus.Archived),
                LessonCount = db.Lessons.Count(lesson => lesson.CourseId == course.Id && lesson.Status != ContentStatus.Archived),
                Pending = db.Enrollments.Count(e => e.CourseId == course.Id && e.Status == EnrollmentStatus.Pending),
                Active = db.Enrollments.Count(e => e.CourseId == course.Id && e.Status == EnrollmentStatus.Approved),
                course.UpdatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        return rows.ConvertAll(row => new TeacherCourseSummaryDto(
            row.Id, row.Title, row.Status, row.SubjectName.Resolve(language.Code), row.GradeName.Resolve(language.Code),
            row.CurriculumName.Resolve(language.Code), row.UnitCount, row.LessonCount, row.Pending, row.Active, row.UpdatedAtUtc));
    }

    public async Task<Result<TeacherCourseDto>> GetAsync(Guid courseId, CancellationToken cancellationToken)
    {
        var course = await OwnedCourses().AsNoTracking()
            .Where(item => item.Id == courseId)
            .Select(item => new
            {
                item.Id,
                item.Title,
                item.Description,
                item.ContentLanguage,
                item.Status,
                item.CurriculumId,
                item.GradeLevelId,
                item.SubjectId,
                item.AcademicYearId,
                SubjectName = item.Subject!.Name,
                GradeName = item.GradeLevel!.Name,
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (course is null)
        {
            return CourseNotFound();
        }

        // Two flat queries instead of a deep Include graph: units, then lessons with resource counts.
        var units = await db.Units.AsNoTracking()
            .Where(unit => unit.CourseId == courseId)
            .OrderBy(unit => unit.SortOrder)
            .Select(unit => new { unit.Id, unit.Title, unit.Description, unit.Status })
            .ToListAsync(cancellationToken);
        var lessons = await db.Lessons.AsNoTracking()
            .Where(lesson => lesson.CourseId == courseId)
            .OrderBy(lesson => lesson.SortOrder)
            .Select(lesson => new TeacherLessonRow(
                lesson.UnitId, lesson.Id, lesson.Title, lesson.Status, lesson.EstimatedMinutes, lesson.Resources.Count))
            .ToListAsync(cancellationToken);
        var lessonsByUnit = lessons.ToLookup(lesson => lesson.UnitId);

        return new TeacherCourseDto(
            course.Id, course.Title, course.Description, course.ContentLanguage, course.Status,
            course.CurriculumId, course.GradeLevelId, course.SubjectId, course.AcademicYearId,
            course.SubjectName.Resolve(language.Code), course.GradeName.Resolve(language.Code),
            units.ConvertAll(unit => new TeacherUnitDto(
                unit.Id, unit.Title, unit.Description, unit.Status,
                lessonsByUnit[unit.Id]
                    .Select(lesson => new TeacherLessonDto(lesson.Id, lesson.Title, lesson.Status, lesson.EstimatedMinutes, lesson.ResourceCount))
                    .ToList())));
    }

    private IQueryable<Course> OwnedCourses()
    {
        var teacherId = currentUser.RequireUserId();
        return db.Courses.Where(course => course.TeacherId == teacherId);
    }

    private async Task<bool> IsValidCatalogPlacementAsync(CreateCourseRequest request, CancellationToken cancellationToken) =>
        await db.GradeSubjects.AnyAsync(
            link => link.GradeLevelId == request.GradeLevelId && link.SubjectId == request.SubjectId
                && link.GradeLevel!.CurriculumId == request.CurriculumId && link.Subject!.IsActive,
            cancellationToken)
        && await db.AcademicYears.AnyAsync(
            year => year.Id == request.AcademicYearId && year.CurriculumId == request.CurriculumId, cancellationToken);

    private static Error CourseNotFound() => Error.NotFound(ErrorCodes.Course.NotFound, "Course not found.");

    private sealed record TeacherLessonRow(Guid UnitId, Guid Id, string Title, ContentStatus Status, short? EstimatedMinutes, int ResourceCount);
}
