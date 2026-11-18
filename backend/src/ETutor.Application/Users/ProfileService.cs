using ETutor.Application.Abstractions;
using ETutor.Application.Common;
using ETutor.Application.Common.Results;
using ETutor.Application.Common.Validation;
using ETutor.Domain.Users;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ETutor.Application.Users;

/// <summary>
/// Reads and updates the caller's own profile. Every query is keyed by the authenticated user ID
/// from <see cref="ICurrentUser"/>, never by an ID from the request, so users can only touch their own data.
/// </summary>
public sealed class ProfileService(
    IAppDbContext db,
    ICurrentUser currentUser,
    IIdentityService identity,
    ProfileQueries profiles,
    StudyPlanRules studyPlan,
    IValidator<UpdateProfileRequest> profileValidator,
    IValidator<UpdateStudentStudyRequest> studyValidator,
    IValidator<UpdateTeacherProfileRequest> teacherValidator)
{
    public async Task<Result<MeDto>> GetMeAsync(CancellationToken cancellationToken)
    {
        var account = await CurrentAccountAsync(cancellationToken);
        return account is null ? ProfileNotFound() : await profiles.GetMeAsync(account, cancellationToken);
    }

    public async Task<Result<MeDto>> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await profileValidator.ValidateToErrorsAsync(request, cancellationToken) is { } invalid)
        {
            return Result<MeDto>.Failure(invalid);
        }

        var account = await CurrentAccountAsync(cancellationToken);
        var profile = account is null
            ? null
            : await db.UserProfiles.Include(user => user.Student).FirstOrDefaultAsync(user => user.Id == account.UserId, cancellationToken);
        if (account is null || profile is null)
        {
            return ProfileNotFound();
        }

        var country = await studyPlan.ResolveCountryAsync(request.CountryCode, cancellationToken);
        if (!country.IsSuccess)
        {
            return Result<MeDto>.Failure(country.Errors);
        }

        // A student's curriculum must stay available in their country.
        if (profile.Student is { } student && country.Value != profile.CountryId)
        {
            var planErrors = await studyPlan.ValidateAsync(
                country.Value, student.CurriculumId, student.GradeLevelId, student.StreamId, [], cancellationToken);
            if (planErrors.Count > 0)
            {
                return Result<MeDto>.Failure(planErrors);
            }
        }

        profile.DisplayName = request.DisplayName.Trim();
        profile.PreferredLanguage = request.PreferredLanguage.ToLowerInvariant();
        profile.TimeZoneId = request.TimeZoneId;
        profile.CountryId = country.Value;
        await db.SaveChangesAsync(cancellationToken);
        return await profiles.GetMeAsync(account, cancellationToken);
    }

    public async Task<Result<MeDto>> UpdateStudentStudyAsync(UpdateStudentStudyRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await studyValidator.ValidateToErrorsAsync(request, cancellationToken) is { } invalid)
        {
            return Result<MeDto>.Failure(invalid);
        }

        var account = await CurrentAccountAsync(cancellationToken);
        if (account is null)
        {
            return ProfileNotFound();
        }

        var student = await db.StudentProfiles
            .Include(profile => profile.Subjects)
            .Include(profile => profile.User)
            .FirstOrDefaultAsync(profile => profile.UserId == account.UserId, cancellationToken);
        if (student is null)
        {
            return Error.Forbidden(ErrorCodes.Profile.NotAStudent, "Only students have a study plan.");
        }

        var planErrors = await studyPlan.ValidateAsync(
            student.User!.CountryId, request.CurriculumId, request.GradeLevelId, request.StreamId, request.SubjectIds, cancellationToken);
        if (planErrors.Count > 0)
        {
            return Result<MeDto>.Failure(planErrors);
        }

        student.CurriculumId = request.CurriculumId;
        student.GradeLevelId = request.GradeLevelId;
        student.StreamId = request.StreamId;
        student.Subjects.Clear();
        foreach (var subjectId in request.SubjectIds)
        {
            student.Subjects.Add(new StudentSubject { StudentId = student.UserId, SubjectId = subjectId });
        }

        await db.SaveChangesAsync(cancellationToken);
        return await profiles.GetMeAsync(account, cancellationToken);
    }

    public async Task<Result<MeDto>> UpdateTeacherProfileAsync(UpdateTeacherProfileRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await teacherValidator.ValidateToErrorsAsync(request, cancellationToken) is { } invalid)
        {
            return Result<MeDto>.Failure(invalid);
        }

        var account = await CurrentAccountAsync(cancellationToken);
        if (account is null)
        {
            return ProfileNotFound();
        }

        var teacher = await db.TeacherProfiles.FirstOrDefaultAsync(profile => profile.UserId == account.UserId, cancellationToken);
        if (teacher is null)
        {
            return Error.Forbidden(ErrorCodes.Profile.NotATeacher, "Only teachers have a teaching profile.");
        }

        teacher.Headline = request.Headline.Trim();
        teacher.Bio = request.Bio.Trim();
        teacher.IsAcceptingStudents = request.IsAcceptingStudents;
        await db.SaveChangesAsync(cancellationToken);
        return await profiles.GetMeAsync(account, cancellationToken);
    }

    private async Task<IdentityAccount?> CurrentAccountAsync(CancellationToken cancellationToken) =>
        currentUser.UserId is { } userId ? await identity.FindActiveAccountAsync(userId, cancellationToken) : null;

    private static Error ProfileNotFound() => Error.NotFound(ErrorCodes.Profile.NotFound, "Profile not found.");
}
