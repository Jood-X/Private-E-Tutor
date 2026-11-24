using System.Net;
using System.Net.Http.Json;
using ETutor.Application.Courses;
using ETutor.Application.Enrollments;
using ETutor.Application.Videos;
using ETutor.Domain.Content;
using ETutor.Infrastructure.Persistence;
using ETutor.IntegrationTests.Infrastructure;
using ETutor.IntegrationTests.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ETutor.IntegrationTests.Courses;

[Collection(ApiTestGroup.Name)]
public sealed class VideoTests(ApiFactory factory)
{
    private static string AddVideoUrl(Guid lessonId) => $"/api/v1/teacher/lessons/{lessonId}/resources/video";

    private static string PlaybackUrl(Guid resourceId) => $"/api/v1/resources/{resourceId}/playback";

    private async Task<(PublishedCourse Course, ResourceDto Video, string AssetId)> CourseWithVideoAsync()
    {
        var course = await factory.PublishedCourseAsync();
        var assetId = FakeVideoProvider.NewId();
        var lesson = await course.TeacherClient.ReadOkAsync<LessonDetailDto>(await course.TeacherClient.PostAsJsonAsync(
            AddVideoUrl(course.LessonId), new VideoResourceRequest($"https://youtu.be/{assetId}?si=share", null, true)));
        return (course, lesson.Resources.Single(resource => resource.Type == ResourceType.Video), assetId);
    }

    [Fact]
    public async Task AddingAVideo_UsesTheProviderTitleAndDetails()
    {
        var (_, video, assetId) = await CourseWithVideoAsync();

        Assert.Equal($"Lecture {assetId}", video.Title);
        Assert.True(video.IsRequired);
        Assert.Null(video.Url);
        Assert.Equal(new ResourceVideoDto(600, $"https://i.ytimg.com/vi/{assetId}/mqdefault.jpg", VideoStatus.Ready), video.Video);
    }

    [Fact]
    public async Task LinkingTheSameVideoTwice_ReusesOneRegisteredVideo_AndKeepsCustomTitles()
    {
        var course = await factory.PublishedCourseAsync();
        var assetId = FakeVideoProvider.NewId();
        await course.TeacherClient.PostAsJsonAsync(
            AddVideoUrl(course.LessonId), new VideoResourceRequest($"https://www.youtube.com/watch?v={assetId}&t=42", null, false));
        var lesson = await course.TeacherClient.ReadOkAsync<LessonDetailDto>(await course.TeacherClient.PostAsJsonAsync(
            AddVideoUrl(course.LessonId), new VideoResourceRequest(assetId, "  Part 2: friction  ", false)));

        Assert.Equal(["Lecture " + assetId, "Part 2: friction"], lesson.Resources.Select(resource => resource.Title));
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(1, await db.VideoAssets.CountAsync(video => video.ProviderAssetId == assetId));
        Assert.Equal(2, factory.Videos.LookupCount(assetId));
    }

    [Theory]
    [InlineData("https://vimeo.com/123456789")]
    [InlineData("https://www.youtube.com/watch?v=short")]
    [InlineData("not a link at all")]
    public async Task LinksThatAreNotYouTubeVideos_AreRejected(string url)
    {
        var course = await factory.PublishedCourseAsync();

        var response = await course.TeacherClient.PostAsJsonAsync(AddVideoUrl(course.LessonId), new VideoResourceRequest(url, null, true));

        await CourseScenario.AssertErrorAsync(response, HttpStatusCode.BadRequest, "video.invalid_url");
    }

    [Theory]
    [InlineData("missing", HttpStatusCode.UnprocessableEntity, "video.not_found")]
    [InlineData("noembed", HttpStatusCode.UnprocessableEntity, "video.not_embeddable")]
    [InlineData("outage", HttpStatusCode.ServiceUnavailable, "video.provider_unavailable")]
    public async Task VideosTheProviderRefuses_AreNotAdded(string behaviour, HttpStatusCode status, string code)
    {
        var course = await factory.PublishedCourseAsync();

        var response = await course.TeacherClient.PostAsJsonAsync(
            AddVideoUrl(course.LessonId), new VideoResourceRequest(FakeVideoProvider.NewId(behaviour), null, true));

        await CourseScenario.AssertErrorAsync(response, status, code);
        var lesson = await course.TeacherClient.ReadOkAsync<LessonDetailDto>(
            await course.TeacherClient.GetAsync($"/api/v1/teacher/lessons/{course.LessonId}"));
        Assert.Empty(lesson.Resources);
    }

    [Fact]
    public async Task AnotherTeachersLesson_IsNotFound_AndTheProviderIsNotAsked()
    {
        var course = await factory.PublishedCourseAsync();
        var otherTeacher = factory.Authorized((await factory.VerifiedTeacherAsync()).AccessToken);
        var assetId = FakeVideoProvider.NewId();

        var response = await otherTeacher.PostAsJsonAsync(AddVideoUrl(course.LessonId), new VideoResourceRequest(assetId, null, true));

        await CourseScenario.AssertErrorAsync(response, HttpStatusCode.NotFound, "course.lesson_not_found");
        Assert.Equal(0, factory.Videos.LookupCount(assetId));
    }

    [Fact]
    public async Task Playback_IsOnlyForTheTeacherAndApprovedStudents()
    {
        var (course, video, assetId) = await CourseWithVideoAsync();
        var url = PlaybackUrl(video.Id);

        var ownerPlayback = await course.TeacherClient.GetAsync(url);
        var playback = await course.TeacherClient.ReadOkAsync<VideoPlaybackDto>(ownerPlayback);
        Assert.Equal(new VideoPlaybackDto(video.Id, "youtube", assetId, video.Title, 600), playback);
        Assert.Contains("no-store", ownerPlayback.Headers.CacheControl!.ToString(), StringComparison.Ordinal);

        var student = factory.Authorized((await factory.CreateClient().RegisterStudentAsync()).AccessToken);
        var otherTeacher = factory.Authorized((await factory.VerifiedTeacherAsync()).AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherTeacher.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync(url)).StatusCode);

        var enrollment = await student.ReadOkAsync<MyEnrollmentDto>(
            await student.PostAsJsonAsync($"/api/v1/courses/{course.CourseId}/enrollments", new EnrollmentRequest(null)));
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync(url)).StatusCode); // pending isn't enough

        await course.TeacherClient.PostAsJsonAsync($"/api/v1/teacher/enrollments/{enrollment.Id}/approve", new DecisionRequest(null));
        Assert.Equal(assetId, (await student.ReadOkAsync<VideoPlaybackDto>(await student.GetAsync(url))).AssetId);

        // The student's lesson shows the video's display details (not the playback ID).
        var lesson = await student.ReadOkAsync<LessonDetailDto>(await student.GetAsync($"/api/v1/learning/lessons/{course.LessonId}"));
        Assert.Equal(600, Assert.Single(lesson.Resources).Video!.DurationSeconds);
    }

    [Fact]
    public async Task AVideoTheProviderNoLongerServes_CannotBePlayed()
    {
        var (course, video, assetId) = await CourseWithVideoAsync();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.VideoAssets.Where(item => item.ProviderAssetId == assetId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, VideoStatus.Unavailable));
        }

        var response = await course.TeacherClient.GetAsync(PlaybackUrl(video.Id));

        await CourseScenario.AssertErrorAsync(response, HttpStatusCode.UnprocessableEntity, "video.unavailable");
    }

    [Fact]
    public async Task OnlyVideoResourcesHavePlayback()
    {
        var course = await factory.PublishedCourseAsync();
        var lesson = await course.TeacherClient.ReadOkAsync<LessonDetailDto>(await course.TeacherClient.PostAsJsonAsync(
            $"/api/v1/teacher/lessons/{course.LessonId}/resources/link", new LinkResourceRequest("Notes", "https://example.com/notes", false)));

        var response = await course.TeacherClient.GetAsync(PlaybackUrl(lesson.Resources.Single().Id));

        await CourseScenario.AssertErrorAsync(response, HttpStatusCode.NotFound, "course.resource_not_found");
    }
}
