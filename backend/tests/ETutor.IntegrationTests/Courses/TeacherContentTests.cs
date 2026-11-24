using System.Net;
using System.Net.Http.Json;
using ETutor.Application.Courses;
using ETutor.Domain.Content;
using ETutor.IntegrationTests.Infrastructure;
using ETutor.IntegrationTests.Users;

namespace ETutor.IntegrationTests.Courses;

[Collection(ApiTestGroup.Name)]
public sealed class TeacherContentTests(ApiFactory factory)
{
    [Fact]
    public async Task CreateCourse_WithGradeFromAnotherCurriculum_Returns400()
    {
        var teacher = await factory.VerifiedTeacherAsync();

        var response = await factory.Authorized(teacher.AccessToken).PostAsJsonAsync(
            "/api/v1/teacher/courses", CourseScenario.NewCourse() with { GradeLevelId = 812 });

        await CourseScenario.AssertErrorAsync(response, HttpStatusCode.BadRequest, "course.invalid_catalog_selection");
    }

    [Fact]
    public async Task PublishingACourse_RequiresAPublishedLesson()
    {
        var teacher = await factory.VerifiedTeacherAsync();
        var client = factory.Authorized(teacher.AccessToken);
        var course = await client.ReadOkAsync<TeacherCourseDto>(await client.PostAsJsonAsync("/api/v1/teacher/courses", CourseScenario.NewCourse()));

        var response = await client.PutAsJsonAsync($"/api/v1/teacher/courses/{course.Id}/status", new ChangeStatusRequest(ContentStatus.Published));

        await CourseScenario.AssertErrorAsync(response, HttpStatusCode.UnprocessableEntity, "course.no_published_lesson");
    }

    [Fact]
    public async Task OtherTeachers_CannotReadOrChangeTheCourse()
    {
        var owner = await factory.PublishedCourseAsync();
        var intruder = factory.Authorized((await factory.VerifiedTeacherAsync()).AccessToken);

        var attempts = new[]
        {
            await intruder.GetAsync($"/api/v1/teacher/courses/{owner.CourseId}"),
            await intruder.PutAsJsonAsync($"/api/v1/teacher/courses/{owner.CourseId}", new UpdateCourseRequest("Hacked", "", "en")),
            await intruder.PutAsJsonAsync($"/api/v1/teacher/courses/{owner.CourseId}/status", new ChangeStatusRequest(ContentStatus.Archived)),
            await intruder.PostAsJsonAsync($"/api/v1/teacher/courses/{owner.CourseId}/units", new UnitRequest("Injected", "")),
            await intruder.PutAsJsonAsync($"/api/v1/teacher/units/{owner.UnitId}", new UnitRequest("Hacked", "")),
            await intruder.PostAsJsonAsync($"/api/v1/teacher/units/{owner.UnitId}/lessons", new LessonRequest("Injected", "", "", null)),
            await intruder.GetAsync($"/api/v1/teacher/lessons/{owner.LessonId}"),
            await intruder.PostAsJsonAsync($"/api/v1/teacher/lessons/{owner.LessonId}/resources/link",
                new LinkResourceRequest("Injected", "https://example.com", false)),
            await intruder.DeleteAsync($"/api/v1/teacher/lessons/{owner.LessonId}"),
        };

        Assert.All(attempts, response => Assert.Equal(HttpStatusCode.NotFound, response.StatusCode));
        var course = await owner.TeacherClient.ReadOkAsync<TeacherCourseDto>(await owner.TeacherClient.GetAsync($"/api/v1/teacher/courses/{owner.CourseId}"));
        Assert.Equal("Physics: Mechanics", course.Title);
        Assert.Equal("Newton's laws", course.Units.Single().Title);
        Assert.Single(course.Units.Single().Lessons);
    }

    [Fact]
    public async Task Students_CannotUseTeacherEndpoints()
    {
        var student = factory.Authorized((await factory.CreateClient().RegisterStudentAsync()).AccessToken);

        var list = await student.GetAsync("/api/v1/teacher/courses");
        var create = await student.PostAsJsonAsync("/api/v1/teacher/courses", CourseScenario.NewCourse());

        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
    }

    [Fact]
    public async Task PublishedLessons_CannotBeDeleted_ButCanBeArchived()
    {
        var owner = await factory.PublishedCourseAsync();
        var client = owner.TeacherClient;

        var delete = await client.DeleteAsync($"/api/v1/teacher/lessons/{owner.LessonId}");
        var archive = await client.PutAsJsonAsync($"/api/v1/teacher/lessons/{owner.LessonId}/status", new ChangeStatusRequest(ContentStatus.Archived));

        await CourseScenario.AssertErrorAsync(delete, HttpStatusCode.UnprocessableEntity, "course.only_draft_can_be_deleted");
        Assert.Equal(HttpStatusCode.OK, archive.StatusCode);
    }

    [Fact]
    public async Task Units_CanBeReordered()
    {
        var owner = await factory.PublishedCourseAsync();
        var client = owner.TeacherClient;
        await client.PostAsJsonAsync($"/api/v1/teacher/courses/{owner.CourseId}/units", new UnitRequest("Energy", ""));
        var outline = await client.ReadOkAsync<TeacherCourseDto>(
            await client.PostAsJsonAsync($"/api/v1/teacher/courses/{owner.CourseId}/units", new UnitRequest("Waves", "")));
        Assert.Equal(["Newton's laws", "Energy", "Waves"], outline.Units.Select(unit => unit.Title));

        var waves = outline.Units.Single(unit => unit.Title == "Waves").Id;
        var moved = await client.ReadOkAsync<TeacherCourseDto>(await client.PutAsJsonAsync($"/api/v1/teacher/units/{waves}/position", new MoveRequest(0)));

        Assert.Equal(["Waves", "Newton's laws", "Energy"], moved.Units.Select(unit => unit.Title));
    }

    [Theory]
    [InlineData("http://example.com/notes")]
    [InlineData("javascript:alert(1)")]
    public async Task Links_MustBeHttps(string url)
    {
        var owner = await factory.PublishedCourseAsync();

        var response = await owner.TeacherClient.PostAsJsonAsync(
            $"/api/v1/teacher/lessons/{owner.LessonId}/resources/link", new LinkResourceRequest("Notes", url, true));

        await CourseScenario.AssertErrorAsync(response, HttpStatusCode.BadRequest, "course.invalid_link");
    }

    [Fact]
    public async Task MyCourses_ShowsCountsForTheTeacherOnly()
    {
        var owner = await factory.PublishedCourseAsync();
        await factory.PublishedCourseAsync(); // another teacher's course must not appear

        var courses = await owner.TeacherClient.ReadOkAsync<List<TeacherCourseSummaryDto>>(await owner.TeacherClient.GetAsync("/api/v1/teacher/courses"));

        var course = Assert.Single(courses);
        Assert.Equal((1, 1, ContentStatus.Published), (course.UnitCount, course.LessonCount, course.Status));
        Assert.Equal("Physics", course.SubjectName);
    }
}
