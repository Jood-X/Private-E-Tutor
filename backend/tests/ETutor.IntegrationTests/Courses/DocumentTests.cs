using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using ETutor.Application.Courses;
using ETutor.Application.Enrollments;
using ETutor.Domain.Content;
using ETutor.IntegrationTests.Infrastructure;
using ETutor.IntegrationTests.Users;

namespace ETutor.IntegrationTests.Courses;

[Collection(ApiTestGroup.Name)]
public sealed class DocumentTests(ApiFactory factory)
{
    private static readonly byte[] TinyPdf = Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj<<>>endobj\ntrailer<<>>\n%%EOF\n");

    private static MultipartFormDataContent Upload(byte[] bytes, string fileName, string contentType = "application/pdf")
    {
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        return new MultipartFormDataContent
        {
            { file, "file", fileName },
            { new StringContent("Worked examples"), "title" },
            { new StringContent("true"), "isRequired" },
        };
    }

    [Fact]
    public async Task NonPdfFiles_AreRejected_EvenWithAPdfNameAndType()
    {
        var course = await factory.PublishedCourseAsync();
        using var content = Upload(Encoding.ASCII.GetBytes("MZ fake executable"), "notes.pdf");

        var response = await course.TeacherClient.PostAsync($"/api/v1/teacher/lessons/{course.LessonId}/resources/document", content);

        await CourseScenario.AssertErrorAsync(response, HttpStatusCode.BadRequest, "course.invalid_document");
    }

    [Fact]
    public async Task Documents_AreDownloadableOnlyByTheTeacherAndApprovedStudents()
    {
        var course = await factory.PublishedCourseAsync();
        using var content = Upload(TinyPdf, "../../secret/examples.pdf");
        var lesson = await course.TeacherClient.ReadOkAsync<LessonDetailDto>(
            await course.TeacherClient.PostAsync($"/api/v1/teacher/lessons/{course.LessonId}/resources/document", content));
        var document = Assert.Single(lesson.Resources);
        Assert.Equal((ResourceType.Document, "examples.pdf", (long?)TinyPdf.Length), (document.Type, document.FileName, document.SizeBytes));
        var url = $"/api/v1/resources/{document.Id}/file";

        // Teacher (owner) can download the exact bytes.
        var teacherDownload = await course.TeacherClient.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, teacherDownload.StatusCode);
        Assert.Equal("application/pdf", teacherDownload.Content.Headers.ContentType!.MediaType);
        Assert.Equal(TinyPdf, await teacherDownload.Content.ReadAsByteArrayAsync());
        Assert.Contains("no-store", teacherDownload.Headers.CacheControl!.ToString(), StringComparison.Ordinal);

        // Anonymous, another teacher and a not-yet-approved student cannot.
        var student = factory.Authorized((await factory.CreateClient().RegisterStudentAsync()).AccessToken);
        var otherTeacher = factory.Authorized((await factory.VerifiedTeacherAsync()).AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherTeacher.GetAsync(url)).StatusCode);
        var enrollment = await student.ReadOkAsync<MyEnrollmentDto>(
            await student.PostAsJsonAsync($"/api/v1/courses/{course.CourseId}/enrollments", new EnrollmentRequest(null)));
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync(url)).StatusCode);

        // Approved student can.
        await course.TeacherClient.PostAsJsonAsync($"/api/v1/teacher/enrollments/{enrollment.Id}/approve", new DecisionRequest(null));
        var studentDownload = await student.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, studentDownload.StatusCode);
        Assert.Equal(TinyPdf, await studentDownload.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task DeletingADocument_RemovesItsDownload()
    {
        var course = await factory.PublishedCourseAsync();
        using var content = Upload(TinyPdf, "examples.pdf");
        var lesson = await course.TeacherClient.ReadOkAsync<LessonDetailDto>(
            await course.TeacherClient.PostAsync($"/api/v1/teacher/lessons/{course.LessonId}/resources/document", content));
        var resourceId = lesson.Resources.Single().Id;

        var afterDelete = await course.TeacherClient.ReadOkAsync<LessonDetailDto>(await course.TeacherClient.DeleteAsync($"/api/v1/teacher/resources/{resourceId}"));

        Assert.Empty(afterDelete.Resources);
        Assert.Equal(HttpStatusCode.NotFound, (await course.TeacherClient.GetAsync($"/api/v1/resources/{resourceId}/file")).StatusCode);
    }
}
