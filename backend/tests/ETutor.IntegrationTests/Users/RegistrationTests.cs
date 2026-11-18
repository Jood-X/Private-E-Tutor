using System.Net;
using System.Net.Http.Json;
using ETutor.Application.Users;
using ETutor.Domain.Users;
using ETutor.IntegrationTests.Infrastructure;

namespace ETutor.IntegrationTests.Users;

[Collection(ApiTestGroup.Name)]
public sealed class RegistrationTests(ApiFactory factory)
{
    private const string StudentRoute = "/api/v1/auth/register/student";

    [Fact]
    public async Task RegisterStudent_Returns201_WithTokensAndStudentProfile()
    {
        var request = AuthClient.Student();

        var auth = await factory.CreateClient().RegisterStudentAsync(request);

        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
        Assert.InRange(auth.AccessTokenExpiresAtUtc, DateTime.UtcNow.AddMinutes(14), DateTime.UtcNow.AddMinutes(16));
        Assert.Equal(request.Email, auth.User.Email);
        Assert.Equal(RoleNames.Student, auth.User.Role);
        Assert.Equal("JO", auth.User.CountryCode);
        Assert.Null(auth.User.Teacher);
        Assert.Equal(AuthClient.TawjihiGrade12, auth.User.Student!.GradeLevelId);
        Assert.Equal([AuthClient.Mathematics, AuthClient.Physics], auth.User.Student.SubjectIds);
    }

    [Fact]
    public async Task RegisterStudent_ResponseNeverContainsCredentialData()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(StudentRoute, AuthClient.Student());
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(AuthClient.Password, body, StringComparison.Ordinal);
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("securityStamp", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegisterStudent_DuplicateEmail_IgnoringCase_Returns409()
    {
        var email = AuthClient.UniqueEmail();
        await factory.CreateClient().RegisterStudentAsync(AuthClient.Student(email));

        var response = await factory.CreateClient().PostAsJsonAsync(StudentRoute, AuthClient.Student(email.ToUpperInvariant()));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = Assert.Single((await response.ReadEnvelopeAsync<object>()).Errors!);
        Assert.Equal("auth.email_taken", error.Code);
        Assert.Equal("email", error.Field);
    }

    [Theory]
    [InlineData("short1")]          // too short
    [InlineData("nodigitsatall")]   // no digit (Identity policy)
    public async Task RegisterStudent_WeakPassword_Returns400(string password)
    {
        var response = await factory.CreateClient().PostAsJsonAsync(StudentRoute, AuthClient.Student() with { Password = password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.ReadEnvelopeAsync<object>()).Errors!;
        Assert.Contains(errors, error => error.Field == "password");
    }

    [Fact]
    public async Task RegisterStudent_FailedRegistration_DoesNotCreateAccount()
    {
        var email = AuthClient.UniqueEmail();
        var invalid = AuthClient.Student(email) with { Password = "nodigitsatall" };
        await factory.CreateClient().PostAsJsonAsync(StudentRoute, invalid);

        // The same email must still be free: nothing was half-created.
        await factory.CreateClient().RegisterStudentAsync(AuthClient.Student(email));
    }

    [Fact]
    public async Task RegisterStudent_GradeFromAnotherCurriculum_Returns400OnGradeField()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            StudentRoute, AuthClient.Student() with { GradeLevelId = AuthClient.CambridgeYear12 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = Assert.Single((await response.ReadEnvelopeAsync<object>()).Errors!);
        Assert.Equal("profile.grade_not_in_curriculum", error.Code);
        Assert.Equal("gradeLevelId", error.Field);
    }

    [Fact]
    public async Task RegisterStudent_CurriculumNotOfferedInCountry_Returns400()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            StudentRoute, AuthClient.Student() with { CurriculumId = AuthClient.SaudiNational });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("profile.curriculum_not_available", (await response.ReadEnvelopeAsync<object>()).Errors![0].Code);
    }

    [Theory]
    [InlineData("Mars/Olympus_Mons", "timeZoneId")]
    [InlineData("fr", "preferredLanguage")]
    public async Task RegisterStudent_InvalidSettings_Return400ForField(string value, string field)
    {
        var request = field == "timeZoneId" ? AuthClient.Student() with { TimeZoneId = value } : AuthClient.Student() with { PreferredLanguage = value };

        var response = await factory.CreateClient().PostAsJsonAsync(StudentRoute, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains((await response.ReadEnvelopeAsync<object>()).Errors!, error => error.Field == field);
    }

    [Fact]
    public async Task RegisterTeacher_StartsAsPendingVerification()
    {
        var auth = await factory.CreateClient().RegisterTeacherAsync();

        Assert.Equal(RoleNames.Teacher, auth.User.Role);
        Assert.Null(auth.User.Student);
        Assert.Equal(TeacherVerificationStatus.Pending, auth.User.Teacher!.VerificationStatus);
        Assert.Equal("ar", auth.User.PreferredLanguage);
    }
}
