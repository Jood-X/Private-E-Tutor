using System.Net;
using System.Net.Http.Json;
using ETutor.Application.Common;
using ETutor.Application.Courses;
using ETutor.Application.Discovery;
using ETutor.Application.Enrollments;
using ETutor.Application.Learning;
using ETutor.Domain.Enrollments;
using ETutor.IntegrationTests.Infrastructure;
using ETutor.IntegrationTests.Users;

namespace ETutor.IntegrationTests.Courses;

[Collection(ApiTestGroup.Name)]
public sealed class EnrollmentAccessTests(ApiFactory factory)
{
    private static readonly EnrollmentRequest Hello = new("Hello, I'd like to join.");

    [Fact]
    public async Task FullFlow_ContentIsHiddenUntilApproval_AndGoneAfterWithdrawal()
    {
        var course = await factory.PublishedCourseAsync();
        var student = factory.Authorized((await factory.CreateClient().RegisterStudentAsync()).AccessToken);

        // Before enrolling: summary yes, outline no, lesson no.
        var preview = await student.ReadOkAsync<StudentCourseDto>(await student.GetAsync($"/api/v1/learning/courses/{course.CourseId}"));
        Assert.Null(preview.Units);
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync($"/api/v1/learning/lessons/{course.LessonId}")).StatusCode);

        // Request: pending still hides the content.
        var requested = await student.ReadOkAsync<MyEnrollmentDto>(
            await student.PostAsJsonAsync($"/api/v1/courses/{course.CourseId}/enrollments", Hello));
        Assert.Equal(EnrollmentStatus.Pending, requested.Status);
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync($"/api/v1/learning/lessons/{course.LessonId}")).StatusCode);

        // Teacher sees the request and approves it.
        var queue = await course.TeacherClient.ReadOkAsync<PagedResult<TeacherEnrollmentDto>>(
            await course.TeacherClient.GetAsync("/api/v1/teacher/enrollments?status=Pending"));
        var request = Assert.Single(queue.Items);
        Assert.Equal("Hello, I'd like to join.", request.RequestMessage);
        await course.TeacherClient.ReadOkAsync<TeacherEnrollmentDto>(
            await course.TeacherClient.PostAsJsonAsync($"/api/v1/teacher/enrollments/{request.Id}/approve", new DecisionRequest("Welcome!")));

        // Approved: outline and lesson are visible.
        var enrolled = await student.ReadOkAsync<StudentCourseDto>(await student.GetAsync($"/api/v1/learning/courses/{course.CourseId}"));
        Assert.Equal("Second law", enrolled.Units!.Single().Lessons.Single().Title);
        var lesson = await student.ReadOkAsync<LessonDetailDto>(await student.GetAsync($"/api/v1/learning/lessons/{course.LessonId}"));
        Assert.Equal("Notes…", lesson.Notes);

        // Withdraw: access is gone again.
        await student.ReadOkAsync<MyEnrollmentDto>(await student.PostAsync($"/api/v1/learning/enrollments/{requested.Id}/withdraw", null));
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync($"/api/v1/learning/lessons/{course.LessonId}")).StatusCode);
    }

    [Fact]
    public async Task ConcurrentDuplicateRequests_CreateExactlyOneEnrollment()
    {
        var course = await factory.PublishedCourseAsync();
        var student = factory.Authorized((await factory.CreateClient().RegisterStudentAsync()).AccessToken);

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
            student.PostAsJsonAsync($"/api/v1/courses/{course.CourseId}/enrollments", Hello)));

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
        Assert.All(responses.Where(response => response.StatusCode != HttpStatusCode.Created),
            response => Assert.Equal(HttpStatusCode.Conflict, response.StatusCode));
    }

    [Fact]
    public async Task Teachers_CannotDecideOnAnotherTeachersEnrollments()
    {
        var course = await factory.PublishedCourseAsync();
        var student = factory.Authorized((await factory.CreateClient().RegisterStudentAsync()).AccessToken);
        var enrollment = await student.ReadOkAsync<MyEnrollmentDto>(
            await student.PostAsJsonAsync($"/api/v1/courses/{course.CourseId}/enrollments", Hello));
        var otherTeacher = factory.Authorized((await factory.VerifiedTeacherAsync()).AccessToken);

        var approve = await otherTeacher.PostAsJsonAsync($"/api/v1/teacher/enrollments/{enrollment.Id}/approve", new DecisionRequest(null));
        var queue = await otherTeacher.ReadOkAsync<PagedResult<TeacherEnrollmentDto>>(await otherTeacher.GetAsync("/api/v1/teacher/enrollments"));

        await CourseScenario.AssertErrorAsync(approve, HttpStatusCode.NotFound, "enrollment.not_found");
        Assert.Empty(queue.Items);
    }

    [Fact]
    public async Task Students_CannotWithdrawSomeoneElsesEnrollment()
    {
        var course = await factory.PublishedCourseAsync();
        var lina = factory.Authorized((await factory.CreateClient().RegisterStudentAsync()).AccessToken);
        var omar = factory.Authorized((await factory.CreateClient().RegisterStudentAsync()).AccessToken);
        var enrollment = await lina.ReadOkAsync<MyEnrollmentDto>(await lina.PostAsJsonAsync($"/api/v1/courses/{course.CourseId}/enrollments", Hello));

        var response = await omar.PostAsync($"/api/v1/learning/enrollments/{enrollment.Id}/withdraw", null);

        await CourseScenario.AssertErrorAsync(response, HttpStatusCode.NotFound, "enrollment.not_found");
    }

    [Fact]
    public async Task RevokedStudents_LoseAccess_AndInvalidTransitionsAreRejected()
    {
        var course = await factory.PublishedCourseAsync();
        var student = factory.Authorized((await factory.CreateClient().RegisterStudentAsync()).AccessToken);
        var enrollment = await student.ReadOkAsync<MyEnrollmentDto>(await student.PostAsJsonAsync($"/api/v1/courses/{course.CourseId}/enrollments", Hello));

        var revokePending = await course.TeacherClient.PostAsJsonAsync($"/api/v1/teacher/enrollments/{enrollment.Id}/revoke", new DecisionRequest(null));
        await CourseScenario.AssertErrorAsync(revokePending, HttpStatusCode.UnprocessableEntity, "enrollment.invalid_transition");

        await course.TeacherClient.PostAsJsonAsync($"/api/v1/teacher/enrollments/{enrollment.Id}/approve", new DecisionRequest(null));
        Assert.Equal(HttpStatusCode.OK, (await student.GetAsync($"/api/v1/learning/lessons/{course.LessonId}")).StatusCode);

        await course.TeacherClient.PostAsJsonAsync($"/api/v1/teacher/enrollments/{enrollment.Id}/revoke", new DecisionRequest("Course full"));
        Assert.Equal(HttpStatusCode.NotFound, (await student.GetAsync($"/api/v1/learning/lessons/{course.LessonId}")).StatusCode);
        var mine = await student.ReadOkAsync<List<MyEnrollmentDto>>(await student.GetAsync("/api/v1/learning/enrollments"));
        Assert.Equal((EnrollmentStatus.Revoked, "Course full"), (mine.Single().Status, mine.Single().DecisionNote));
    }

    [Fact]
    public async Task TeachersNotAcceptingStudents_CannotBeRequested()
    {
        var course = await factory.PublishedCourseAsync(acceptingStudents: false);
        var student = factory.Authorized((await factory.CreateClient().RegisterStudentAsync()).AccessToken);

        var response = await student.PostAsJsonAsync($"/api/v1/courses/{course.CourseId}/enrollments", Hello);

        await CourseScenario.AssertErrorAsync(response, HttpStatusCode.UnprocessableEntity, "enrollment.not_accepting_students");
    }

    [Fact]
    public async Task Discovery_ListsOnlyVerifiedTeachersWithMatchingPublishedCourses()
    {
        var chemistry = await factory.PublishedCourseAsync(subjectId: CourseScenario.Chemistry);
        var unverified = await factory.CreateClient().RegisterTeacherAsync(); // pending verification
        var student = factory.Authorized((await factory.CreateClient().RegisterStudentAsync()).AccessToken);

        var bySubject = await student.ReadOkAsync<PagedResult<TeacherCardDto>>(
            await student.GetAsync($"/api/v1/teachers?subjectId={CourseScenario.Chemistry}&pageSize=50"));
        var unverifiedProfile = await student.GetAsync($"/api/v1/teachers/{unverified.User.Id}");

        var card = Assert.Single(bySubject.Items, item => item.Id == chemistry.Teacher.User.Id);
        Assert.Contains("Chemistry", card.Subjects);
        Assert.DoesNotContain(bySubject.Items, item => item.Id == unverified.User.Id);
        Assert.Equal(HttpStatusCode.NotFound, unverifiedProfile.StatusCode);
    }

    [Fact]
    public async Task TeacherProfile_ShowsPublishedCoursesAndMyEnrollmentStatus()
    {
        var course = await factory.PublishedCourseAsync();
        var student = factory.Authorized((await factory.CreateClient().RegisterStudentAsync()).AccessToken);
        await student.PostAsJsonAsync($"/api/v1/courses/{course.CourseId}/enrollments", Hello);

        var profile = await student.ReadOkAsync<TeacherDetailDto>(await student.GetAsync($"/api/v1/teachers/{course.Teacher.User.Id}"));

        var listed = Assert.Single(profile.Courses);
        Assert.Equal((course.CourseId, 1, EnrollmentStatus.Pending), (listed.Id, listed.LessonCount, listed.MyEnrollmentStatus));
    }

    [Fact]
    public async Task TeacherQueue_IsPaginated()
    {
        var course = await factory.PublishedCourseAsync();
        for (var index = 0; index < 3; index++)
        {
            var student = factory.Authorized((await factory.CreateClient().RegisterStudentAsync()).AccessToken);
            await student.PostAsJsonAsync($"/api/v1/courses/{course.CourseId}/enrollments", Hello);
        }

        var page = await course.TeacherClient.ReadOkAsync<PagedResult<TeacherEnrollmentDto>>(
            await course.TeacherClient.GetAsync("/api/v1/teacher/enrollments?page=2&pageSize=2"));

        Assert.Equal((2, 2, 3, 2), (page.Page, page.PageSize, page.TotalCount, page.TotalPages));
        Assert.Single(page.Items);
    }
}
