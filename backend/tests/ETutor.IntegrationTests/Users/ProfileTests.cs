using System.Net;
using System.Net.Http.Json;
using ETutor.Application.Users;
using ETutor.IntegrationTests.Infrastructure;

namespace ETutor.IntegrationTests.Users;

[Collection(ApiTestGroup.Name)]
public sealed class ProfileTests(ApiFactory factory)
{
    private const string MeRoute = "/api/v1/me";
    private const int Chemistry = 3;

    [Fact]
    public async Task UpdateProfile_ChangesOnlyTheCallersOwnProfile()
    {
        var lina = await factory.CreateClient().RegisterStudentAsync();
        var omar = await factory.CreateClient().RegisterStudentAsync();

        var response = await factory.Authorized(lina.AccessToken)
            .PutAsJsonAsync(MeRoute, new UpdateProfileRequest("Lina M.", "ar", "Asia/Dubai", "JO"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.ReadEnvelopeAsync<MeDto>()).Data!;
        Assert.Equal(("Lina M.", "ar", "Asia/Dubai", "JO"), (updated.DisplayName, updated.PreferredLanguage, updated.TimeZoneId, updated.CountryCode));

        var omarProfile = (await (await factory.Authorized(omar.AccessToken).GetAsync(MeRoute)).ReadEnvelopeAsync<MeDto>()).Data!;
        Assert.Equal(("Lina", "en", "Asia/Amman"), (omarProfile.DisplayName, omarProfile.PreferredLanguage, omarProfile.TimeZoneId)); // untouched
    }

    [Fact]
    public async Task UpdateProfile_MovingToACountryWithoutTheCurriculum_Returns400()
    {
        var student = await factory.CreateClient().RegisterStudentAsync(); // Tawjihi is only offered in Jordan

        var response = await factory.Authorized(student.AccessToken)
            .PutAsJsonAsync(MeRoute, new UpdateProfileRequest("Lina", "en", "Asia/Riyadh", "SA"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("profile.curriculum_not_available", (await response.ReadEnvelopeAsync<object>()).Errors![0].Code);
    }

    [Fact]
    public async Task UpdateStudy_ReplacesTheStudyPlan()
    {
        var student = await factory.CreateClient().RegisterStudentAsync();

        var response = await factory.Authorized(student.AccessToken).PutAsJsonAsync(
            $"{MeRoute}/student", new UpdateStudentStudyRequest(AuthClient.Tawjihi, 111, null, [Chemistry]));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var study = (await response.ReadEnvelopeAsync<MeDto>()).Data!.Student!;
        Assert.Equal((111, (int?)null), (study.GradeLevelId, study.StreamId));
        Assert.Equal([Chemistry], study.SubjectIds);
    }

    [Fact]
    public async Task UpdateStudy_AsTeacher_IsForbidden()
    {
        var teacher = await factory.CreateClient().RegisterTeacherAsync();

        var response = await factory.Authorized(teacher.AccessToken).PutAsJsonAsync(
            $"{MeRoute}/student", new UpdateStudentStudyRequest(AuthClient.Tawjihi, AuthClient.TawjihiGrade12, null, []));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("common.forbidden", (await response.ReadEnvelopeAsync<object>()).Errors![0].Code);
    }

    [Fact]
    public async Task UpdateTeacherProfile_AsStudent_IsForbidden_AndAsTeacherSucceeds()
    {
        var student = await factory.CreateClient().RegisterStudentAsync();
        var teacher = await factory.CreateClient().RegisterTeacherAsync();
        var request = new UpdateTeacherProfileRequest("Senior physics teacher", "Ten years of Tawjihi preparation.", false);

        var asStudent = await factory.Authorized(student.AccessToken).PutAsJsonAsync($"{MeRoute}/teacher", request);
        var asTeacher = await factory.Authorized(teacher.AccessToken).PutAsJsonAsync($"{MeRoute}/teacher", request);

        Assert.Equal(HttpStatusCode.Forbidden, asStudent.StatusCode);
        Assert.Equal(HttpStatusCode.OK, asTeacher.StatusCode);
        var info = (await asTeacher.ReadEnvelopeAsync<MeDto>()).Data!.Teacher!;
        Assert.Equal(("Senior physics teacher", false), (info.Headline, info.IsAcceptingStudents));
    }
}
