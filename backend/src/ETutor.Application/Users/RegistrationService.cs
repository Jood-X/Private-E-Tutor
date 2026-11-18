using ETutor.Application.Abstractions;
using ETutor.Application.Common.Results;
using ETutor.Application.Common.Validation;
using ETutor.Domain.Users;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ETutor.Application.Users;

/// <summary>
/// Creates student and teacher accounts. The identity account, profile rows and the first refresh
/// token are written in one transaction, so a failure never leaves a half-created user.
/// </summary>
public sealed class RegistrationService(
    IAppDbContext db,
    IIdentityService identity,
    StudyPlanRules studyPlan,
    TokenIssuer tokenIssuer,
    ProfileQueries profiles,
    IValidator<RegisterStudentRequest> studentValidator,
    IValidator<RegisterTeacherRequest> teacherValidator,
    ILogger<RegistrationService> logger)
{
    public async Task<Result<AuthResponse>> RegisterStudentAsync(RegisterStudentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await studentValidator.ValidateToErrorsAsync(request, cancellationToken) is { } invalid)
        {
            return Result<AuthResponse>.Failure(invalid);
        }

        var country = await studyPlan.ResolveCountryAsync(request.CountryCode, cancellationToken);
        if (!country.IsSuccess)
        {
            return Result<AuthResponse>.Failure(country.Errors);
        }

        var planErrors = await studyPlan.ValidateAsync(
            country.Value, request.CurriculumId, request.GradeLevelId, request.StreamId, request.SubjectIds, cancellationToken);
        if (planErrors.Count > 0)
        {
            return Result<AuthResponse>.Failure(planErrors);
        }

        return await db.ExecuteInTransactionAsync(async ct =>
        {
            var account = await identity.CreateAccountAsync(request.Email, request.Password, RoleNames.Student, ct);
            if (!account.IsSuccess)
            {
                return Result<AuthResponse>.Failure(account.Errors);
            }

            var userId = account.Value.UserId;
            db.UserProfiles.Add(NewProfile(userId, request.DisplayName, request.PreferredLanguage, request.TimeZoneId, country.Value));
            var student = new StudentProfile
            {
                UserId = userId,
                CurriculumId = request.CurriculumId,
                GradeLevelId = request.GradeLevelId,
                StreamId = request.StreamId,
            };
            foreach (var subjectId in request.SubjectIds)
            {
                student.Subjects.Add(new StudentSubject { StudentId = userId, SubjectId = subjectId });
            }

            db.StudentProfiles.Add(student);
            await db.SaveChangesAsync(ct);
            UserLog.Registered(logger, userId, RoleNames.Student);
            return await SignInAsync(account.Value, ct);
        }, cancellationToken);
    }

    public async Task<Result<AuthResponse>> RegisterTeacherAsync(RegisterTeacherRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await teacherValidator.ValidateToErrorsAsync(request, cancellationToken) is { } invalid)
        {
            return Result<AuthResponse>.Failure(invalid);
        }

        var country = await studyPlan.ResolveCountryAsync(request.CountryCode, cancellationToken);
        if (!country.IsSuccess)
        {
            return Result<AuthResponse>.Failure(country.Errors);
        }

        return await db.ExecuteInTransactionAsync(async ct =>
        {
            var account = await identity.CreateAccountAsync(request.Email, request.Password, RoleNames.Teacher, ct);
            if (!account.IsSuccess)
            {
                return Result<AuthResponse>.Failure(account.Errors);
            }

            var userId = account.Value.UserId;
            db.UserProfiles.Add(NewProfile(userId, request.DisplayName, request.PreferredLanguage, request.TimeZoneId, country.Value));

            // New teachers start Pending and stay hidden from discovery until verified (decision D5).
            db.TeacherProfiles.Add(new TeacherProfile { UserId = userId, Headline = request.Headline.Trim() });
            await db.SaveChangesAsync(ct);
            UserLog.Registered(logger, userId, RoleNames.Teacher);
            return await SignInAsync(account.Value, ct);
        }, cancellationToken);
    }

    private async Task<Result<AuthResponse>> SignInAsync(IdentityAccount account, CancellationToken cancellationToken)
    {
        var tokens = await tokenIssuer.IssueAsync(account, familyId: Guid.NewGuid(), cancellationToken);
        var me = await profiles.GetMeAsync(account, cancellationToken);
        return me.IsSuccess
            ? new AuthResponse(tokens.Access.Value, tokens.Access.ExpiresAtUtc, tokens.Refresh.Value, tokens.Refresh.ExpiresAtUtc, me.Value)
            : Result<AuthResponse>.Failure(me.Errors);
    }

    private static UserProfile NewProfile(Guid userId, string displayName, string language, string timeZoneId, int countryId) => new()
    {
        Id = userId,
        DisplayName = displayName.Trim(),
        PreferredLanguage = language.ToLowerInvariant(),
        TimeZoneId = timeZoneId,
        CountryId = countryId,
    };
}
