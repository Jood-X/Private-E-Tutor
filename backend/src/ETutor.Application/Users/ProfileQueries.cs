using ETutor.Application.Abstractions;
using ETutor.Application.Common;
using ETutor.Application.Common.Results;
using ETutor.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace ETutor.Application.Users;

/// <summary>Builds <see cref="MeDto"/> from the identity account plus one projected profile query.</summary>
public sealed class ProfileQueries(IAppDbContext db)
{
    public async Task<Result<MeDto>> GetMeAsync(IdentityAccount account, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);
        var profile = await db.UserProfiles.AsNoTracking()
            .Where(user => user.Id == account.UserId)
            .Select(user => new
            {
                user.DisplayName,
                user.PreferredLanguage,
                user.TimeZoneId,
                CountryCode = user.Country!.IsoCode,
                Student = user.Student == null
                    ? null
                    : new
                    {
                        user.Student.CurriculumId,
                        user.Student.GradeLevelId,
                        user.Student.StreamId,
                        SubjectIds = user.Student.Subjects.OrderBy(subject => subject.SubjectId)
                            .Select(subject => subject.SubjectId).ToList(),
                    },
                Teacher = user.Teacher == null
                    ? null
                    : new TeacherInfoDto(
                        user.Teacher.Headline,
                        user.Teacher.Bio,
                        user.Teacher.VerificationStatus,
                        user.Teacher.IsAcceptingStudents),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return Error.NotFound(ErrorCodes.Profile.NotFound, "Profile not found.");
        }

        return new MeDto(
            account.UserId,
            account.Email,
            PrimaryRole(account.Roles),
            profile.DisplayName,
            profile.PreferredLanguage,
            profile.TimeZoneId,
            profile.CountryCode,
            profile.Student is null
                ? null
                : new StudentStudyDto(profile.Student.CurriculumId, profile.Student.GradeLevelId,
                    profile.Student.StreamId, profile.Student.SubjectIds),
            profile.Teacher);
    }

    private static string PrimaryRole(IReadOnlyList<string> roles) =>
        roles.Contains(RoleNames.Teacher) ? RoleNames.Teacher
        : roles.Contains(RoleNames.Student) ? RoleNames.Student
        : roles.Count > 0 ? roles[0] : string.Empty;
}
