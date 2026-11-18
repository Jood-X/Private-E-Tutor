using ETutor.Domain.Users;

namespace ETutor.Application.Users;

// ---- Requests ----

public sealed record RegisterStudentRequest(
    string Email,
    string Password,
    string DisplayName,
    string PreferredLanguage,
    string TimeZoneId,
    string CountryCode,
    int CurriculumId,
    int GradeLevelId,
    int? StreamId,
    IReadOnlyList<int> SubjectIds);

public sealed record RegisterTeacherRequest(
    string Email,
    string Password,
    string DisplayName,
    string PreferredLanguage,
    string TimeZoneId,
    string CountryCode,
    string Headline);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record UpdateProfileRequest(string DisplayName, string PreferredLanguage, string TimeZoneId, string CountryCode);

public sealed record UpdateStudentStudyRequest(int CurriculumId, int GradeLevelId, int? StreamId, IReadOnlyList<int> SubjectIds);

public sealed record UpdateTeacherProfileRequest(string Headline, string Bio, bool IsAcceptingStudents);

// ---- Responses ----

/// <summary>Tokens plus the signed-in user's profile, returned by register, login and refresh.</summary>
public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    MeDto User);

/// <summary>The caller's own profile. Exactly one of <see cref="Student"/> / <see cref="Teacher"/> is set.</summary>
public sealed record MeDto(
    Guid Id,
    string Email,
    string Role,
    string DisplayName,
    string PreferredLanguage,
    string TimeZoneId,
    string CountryCode,
    StudentStudyDto? Student,
    TeacherInfoDto? Teacher);

public sealed record StudentStudyDto(int CurriculumId, int GradeLevelId, int? StreamId, IReadOnlyList<int> SubjectIds);

public sealed record TeacherInfoDto(
    string Headline,
    string Bio,
    TeacherVerificationStatus VerificationStatus,
    bool IsAcceptingStudents);
