using System.Net;
using System.Net.Http.Json;
using ETutor.Application.Courses;
using ETutor.Application.Users;
using ETutor.Domain.Content;
using ETutor.Domain.Users;
using ETutor.Infrastructure.Persistence;
using ETutor.IntegrationTests.Infrastructure;
using ETutor.IntegrationTests.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ETutor.IntegrationTests.Courses;

/// <summary>A teacher with one fully published course (unit + lesson), built through the real API.</summary>
internal sealed record PublishedCourse(AuthResponse Teacher, HttpClient TeacherClient, Guid CourseId, Guid UnitId, Guid LessonId);

internal static class CourseScenario
{
    public const int Tawjihi = 1;
    public const int TawjihiGrade12 = 112;
    public const int Physics = 2;
    public const int Chemistry = 3;
    public const int AcademicYear = 1;

    public static CreateCourseRequest NewCourse(int subjectId = Physics) =>
        new("Physics: Mechanics", "Forces, motion and energy.", "en", Tawjihi, TawjihiGrade12, subjectId, AcademicYear);

    public static async Task<AuthResponse> VerifiedTeacherAsync(this ApiFactory factory, bool acceptingStudents = true)
    {
        var teacher = await factory.CreateClient().RegisterTeacherAsync();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.TeacherProfiles.Where(profile => profile.UserId == teacher.User.Id).ExecuteUpdateAsync(setters => setters
            .SetProperty(profile => profile.VerificationStatus, TeacherVerificationStatus.Verified)
            .SetProperty(profile => profile.IsAcceptingStudents, acceptingStudents));
        return teacher;
    }

    public static async Task<PublishedCourse> PublishedCourseAsync(this ApiFactory factory, int subjectId = Physics, bool acceptingStudents = true)
    {
        var teacher = await factory.VerifiedTeacherAsync(acceptingStudents);
        var client = factory.Authorized(teacher.AccessToken);

        var course = await client.ReadOkAsync<TeacherCourseDto>(await client.PostAsJsonAsync("/api/v1/teacher/courses", NewCourse(subjectId)));
        var withUnit = await client.ReadOkAsync<TeacherCourseDto>(
            await client.PostAsJsonAsync($"/api/v1/teacher/courses/{course.Id}/units", new UnitRequest("Newton's laws", "")));
        var unitId = withUnit.Units.Single().Id;
        var withLesson = await client.ReadOkAsync<TeacherCourseDto>(
            await client.PostAsJsonAsync($"/api/v1/teacher/units/{unitId}/lessons", new LessonRequest("Second law", "F = ma", "Notes…", 25)));
        var lessonId = withLesson.Units.Single().Lessons.Single().Id;

        await client.ReadOkAsync<TeacherCourseDto>(await client.PutAsJsonAsync($"/api/v1/teacher/lessons/{lessonId}/status", new ChangeStatusRequest(ContentStatus.Published)));
        await client.ReadOkAsync<TeacherCourseDto>(await client.PutAsJsonAsync($"/api/v1/teacher/units/{unitId}/status", new ChangeStatusRequest(ContentStatus.Published)));
        await client.ReadOkAsync<TeacherCourseDto>(await client.PutAsJsonAsync($"/api/v1/teacher/courses/{course.Id}/status", new ChangeStatusRequest(ContentStatus.Published)));
        return new PublishedCourse(teacher, client, course.Id, unitId, lessonId);
    }

    /// <summary>Asserts a 2xx response and returns its data.</summary>
    public static async Task<T> ReadOkAsync<T>(this HttpClient client, HttpResponseMessage response)
    {
        _ = client;
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {body}");
        return (await response.ReadEnvelopeAsync<T>()).Data!;
    }

    public static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, (await response.ReadEnvelopeAsync<object>()).Errors![0].Code);
    }
}
