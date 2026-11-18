using System.Security.Cryptography;
using ETutor.Domain.Users;
using ETutor.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ETutor.Infrastructure.Persistence.Seed;

/// <summary>
/// Demo accounts for local development (`dotnet run -- seed-dev`, Development only). Idempotent: existing
/// accounts are skipped. Passwords are random, returned once to the caller and never stored in the repo.
/// Emails use the reserved .test domain so they can never reach a real mailbox.
/// </summary>
public sealed class DevDataSeeder(AppDbContext db, UserManager<ApplicationUser> users)
{
    private const int JordanId = 1;
    private const int TawjihiId = 1;
    private const int TawjihiGrade12Id = 112;
    private const int ScientificStreamId = 1;
    private const int CambridgeId = 8;
    private const int CambridgeYear12Id = 812;
    private const int UaeId = 3;

    public sealed record SeededAccount(string Email, string Role, string Password);

    public async Task<IReadOnlyList<SeededAccount>> SeedAsync(CancellationToken cancellationToken)
    {
        var created = new List<SeededAccount>();
        await AddTeacherAsync("khaled.teacher@etutor.test", "Khaled Haddad", "Physics teacher · 12 years of Tawjihi experience", JordanId, "Asia/Amman", created, cancellationToken);
        await AddTeacherAsync("rana.teacher@etutor.test", "Rana Saleh", "Chemistry & Biology for IGCSE and Tawjihi", UaeId, "Asia/Dubai", created, cancellationToken);
        await AddStudentAsync("lina.student@etutor.test", "Lina", JordanId, "Asia/Amman", TawjihiId, TawjihiGrade12Id, ScientificStreamId, [1, 2, 3], created, cancellationToken);
        await AddStudentAsync("omar.student@etutor.test", "Omar", JordanId, "Asia/Amman", TawjihiId, TawjihiGrade12Id, ScientificStreamId, [1, 2], created, cancellationToken);
        await AddStudentAsync("sara.student@etutor.test", "Sara", UaeId, "Asia/Dubai", CambridgeId, CambridgeYear12Id, null, [2, 3, 4], created, cancellationToken);
        return created;
    }

    private async Task AddTeacherAsync(string email, string name, string headline, int countryId, string timeZone,
        List<SeededAccount> created, CancellationToken cancellationToken)
    {
        var user = await CreateUserAsync(email, RoleNames.Teacher, created);
        if (user is null)
        {
            return;
        }

        db.UserProfiles.Add(Profile(user.Id, name, countryId, timeZone));
        db.TeacherProfiles.Add(new TeacherProfile
        {
            UserId = user.Id,
            Headline = headline,
            Bio = "Demo teacher account created by seed-dev.",
            VerificationStatus = TeacherVerificationStatus.Verified,
            VerifiedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task AddStudentAsync(string email, string name, int countryId, string timeZone, int curriculumId,
        int gradeId, int? streamId, int[] subjectIds, List<SeededAccount> created, CancellationToken cancellationToken)
    {
        var user = await CreateUserAsync(email, RoleNames.Student, created);
        if (user is null)
        {
            return;
        }

        db.UserProfiles.Add(Profile(user.Id, name, countryId, timeZone));
        var student = new StudentProfile { UserId = user.Id, CurriculumId = curriculumId, GradeLevelId = gradeId, StreamId = streamId };
        foreach (var subjectId in subjectIds)
        {
            student.Subjects.Add(new StudentSubject { StudentId = user.Id, SubjectId = subjectId });
        }

        db.StudentProfiles.Add(student);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<ApplicationUser?> CreateUserAsync(string email, string role, List<SeededAccount> created)
    {
        if (await users.FindByEmailAsync(email) is not null)
        {
            return null;
        }

        // Random, policy-compliant password (letters + digits), shown once.
        var password = $"Dev-{Convert.ToHexString(RandomNumberGenerator.GetBytes(6)).ToLowerInvariant()}9";
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = email, Email = email, EmailConfirmed = true };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Could not seed {email}: {string.Join(", ", result.Errors.Select(e => e.Code))}");
        }

        await users.AddToRoleAsync(user, role);
        created.Add(new SeededAccount(email, role, password));
        return user;
    }

    private static UserProfile Profile(Guid id, string name, int countryId, string timeZone) => new()
    {
        Id = id,
        DisplayName = name,
        PreferredLanguage = "en",
        TimeZoneId = timeZone,
        CountryId = countryId,
    };
}
