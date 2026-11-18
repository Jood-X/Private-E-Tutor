using System.Net.Http.Headers;
using System.Net.Http.Json;
using ETutor.Application.Users;
using ETutor.IntegrationTests.Infrastructure;

namespace ETutor.IntegrationTests.Users;

/// <summary>Helpers that drive the auth endpoints exactly as a client would.</summary>
internal static class AuthClient
{
    public const string Password = "Correct-horse-42";
    public const int Tawjihi = 1;
    public const int TawjihiGrade12 = 112;
    public const int ScientificStream = 1;
    public const int CambridgeYear12 = 812;
    public const int SaudiNational = 2;
    public const int Mathematics = 1;
    public const int Physics = 2;

    public static string UniqueEmail(string prefix = "user") => $"{prefix}.{Guid.NewGuid():N}@etutor.test";

    public static RegisterStudentRequest Student(string? email = null) => new(
        email ?? UniqueEmail("student"), Password, "Lina", "en", "Asia/Amman", "JO",
        Tawjihi, TawjihiGrade12, ScientificStream, [Mathematics, Physics]);

    public static RegisterTeacherRequest Teacher(string? email = null) => new(
        email ?? UniqueEmail("teacher"), Password, "Khaled Haddad", "ar", "Asia/Amman", "JO", "Physics teacher");

    public static async Task<AuthResponse> RegisterStudentAsync(this HttpClient client, RegisterStudentRequest? request = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/register/student", request ?? Student());
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        return (await response.ReadEnvelopeAsync<AuthResponse>()).Data!;
    }

    public static async Task<AuthResponse> RegisterTeacherAsync(this HttpClient client, RegisterTeacherRequest? request = null)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/register/teacher", request ?? Teacher());
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        return (await response.ReadEnvelopeAsync<AuthResponse>()).Data!;
    }

    public static HttpClient Authorized(this ApiFactory factory, string accessToken)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }
}
