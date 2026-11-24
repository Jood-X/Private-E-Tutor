using ETutor.Application.Abstractions;
using ETutor.Application.Common;
using ETutor.Application.Common.Results;
using ETutor.Domain.Content;
using ETutor.Domain.Enrollments;
using ETutor.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace ETutor.Application.Discovery;

/// <summary>Optional filters; omitted ones match everything. <c>Search</c> matches the teacher's name or headline.</summary>
public sealed record TeacherSearch(int? CurriculumId = null, int? GradeLevelId = null, int? SubjectId = null, string? Search = null);

public sealed record TeacherCardDto(Guid Id, string DisplayName, string Headline, string CountryCode, IReadOnlyList<string> Subjects, int CourseCount);

public sealed record TeacherDetailDto(
    Guid Id,
    string DisplayName,
    string Headline,
    string Bio,
    string CountryCode,
    bool IsAcceptingStudents,
    IReadOnlyList<PublicCourseDto> Courses);

/// <summary>A published course as students see it before enrolling. <c>MyEnrollmentStatus</c> is the caller's active one, if any.</summary>
public sealed record PublicCourseDto(
    Guid Id,
    string Title,
    string Description,
    string ContentLanguage,
    string SubjectName,
    string GradeLevelName,
    string CurriculumName,
    int UnitCount,
    int LessonCount,
    EnrollmentStatus? MyEnrollmentStatus);

/// <summary>
/// Finds teachers for students. Only verified teachers who accept students and have at least one published
/// course matching the filters are listed (decision D5).
/// </summary>
public sealed class DiscoveryService(IAppDbContext db, ICurrentUser currentUser, IRequestLanguage language)
{
    private const int MaxSearchLength = 100;

    public async Task<PagedResult<TeacherCardDto>> SearchAsync(TeacherSearch search, PageRequest page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(search);
        var courses = MatchingCourses(search);
        var teachers = db.TeacherProfiles.AsNoTracking()
            .Where(teacher => teacher.VerificationStatus == TeacherVerificationStatus.Verified && teacher.IsAcceptingStudents)
            .Where(teacher => courses.Any(course => course.TeacherId == teacher.UserId));

        var text = search.Search?.Trim();
        if (!string.IsNullOrEmpty(text))
        {
            text = text.Length > MaxSearchLength ? text[..MaxSearchLength] : text;
            teachers = teachers.Where(teacher => teacher.User!.DisplayName.Contains(text) || teacher.Headline.Contains(text));
        }

        var pageOfTeachers = await teachers
            .OrderBy(teacher => teacher.User!.DisplayName)
            .ThenBy(teacher => teacher.UserId)
            .Select(teacher => new { teacher.UserId, teacher.User!.DisplayName, teacher.Headline, CountryCode = teacher.User.Country!.IsoCode })
            .ToPagedAsync(page, cancellationToken);

        // One query for the subjects of the whole page (no N+1).
        var teacherIds = pageOfTeachers.Items.Select(teacher => teacher.UserId).ToList();
        var subjectRows = await courses
            .Where(course => teacherIds.Contains(course.TeacherId))
            .Select(course => new { course.TeacherId, course.SubjectId, course.Subject!.Name })
            .ToListAsync(cancellationToken);
        var subjectsByTeacher = subjectRows.ToLookup(row => row.TeacherId);

        return pageOfTeachers.Map(teacher =>
        {
            var rows = subjectsByTeacher[teacher.UserId].ToList();
            var subjects = rows.DistinctBy(row => row.SubjectId).Select(row => row.Name.Resolve(language.Code)).Order().ToList();
            return new TeacherCardDto(teacher.UserId, teacher.DisplayName, teacher.Headline, teacher.CountryCode, subjects, rows.Count);
        });
    }

    public async Task<Result<TeacherDetailDto>> GetTeacherAsync(Guid teacherId, CancellationToken cancellationToken)
    {
        var teacher = await db.TeacherProfiles.AsNoTracking()
            .Where(item => item.UserId == teacherId && item.VerificationStatus == TeacherVerificationStatus.Verified)
            .Select(item => new { item.UserId, item.User!.DisplayName, item.Headline, item.Bio, CountryCode = item.User.Country!.IsoCode, item.IsAcceptingStudents })
            .FirstOrDefaultAsync(cancellationToken);
        if (teacher is null)
        {
            return Error.NotFound(ErrorCodes.Teacher.NotFound, "Teacher not found.");
        }

        var studentId = currentUser.UserId;
        var courses = await db.Courses.AsNoTracking()
            .Where(course => course.TeacherId == teacherId && course.Status == ContentStatus.Published)
            .OrderBy(course => course.Title)
            .Select(course => new
            {
                course.Id,
                course.Title,
                course.Description,
                course.ContentLanguage,
                SubjectName = course.Subject!.Name,
                GradeName = course.GradeLevel!.Name,
                CurriculumName = course.Curriculum!.Name,
                UnitCount = course.Units.Count(unit => unit.Status == ContentStatus.Published),
                LessonCount = db.Lessons.Count(lesson => lesson.CourseId == course.Id && lesson.Status == ContentStatus.Published
                    && lesson.Unit!.Status == ContentStatus.Published),
                MyStatus = db.Enrollments
                    .Where(e => e.CourseId == course.Id && e.StudentId == studentId
                        && (e.Status == EnrollmentStatus.Pending || e.Status == EnrollmentStatus.Approved))
                    .Select(e => (EnrollmentStatus?)e.Status)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        return new TeacherDetailDto(
            teacher.UserId, teacher.DisplayName, teacher.Headline, teacher.Bio, teacher.CountryCode, teacher.IsAcceptingStudents,
            courses.ConvertAll(course => new PublicCourseDto(
                course.Id, course.Title, course.Description, course.ContentLanguage,
                course.SubjectName.Resolve(language.Code), course.GradeName.Resolve(language.Code), course.CurriculumName.Resolve(language.Code),
                course.UnitCount, course.LessonCount, course.MyStatus)));
    }

    private IQueryable<Course> MatchingCourses(TeacherSearch search)
    {
        var courses = db.Courses.AsNoTracking().Where(course => course.Status == ContentStatus.Published);
        if (search.CurriculumId is { } curriculumId)
        {
            courses = courses.Where(course => course.CurriculumId == curriculumId);
        }

        if (search.GradeLevelId is { } gradeLevelId)
        {
            courses = courses.Where(course => course.GradeLevelId == gradeLevelId);
        }

        if (search.SubjectId is { } subjectId)
        {
            courses = courses.Where(course => course.SubjectId == subjectId);
        }

        return courses;
    }
}
