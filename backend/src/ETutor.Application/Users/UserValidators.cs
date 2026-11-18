using ETutor.Application.Common.Validation;
using FluentValidation;

namespace ETutor.Application.Users;

// Shape-only rules. Rules that need the database (e.g. "grade belongs to curriculum") live in StudyPlanRules.

internal static class CredentialRules
{
    public const int MaxEmailLength = 256;
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;
    public const int MaxSubjects = 20;

    public static void Email<T>(IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MaximumLength(MaxEmailLength).EmailAddress();

    public static void NewPassword<T>(IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().MinimumLength(MinPasswordLength).MaximumLength(MaxPasswordLength);
}

internal sealed class RegisterStudentRequestValidator : AbstractValidator<RegisterStudentRequest>
{
    public RegisterStudentRequestValidator()
    {
        CredentialRules.Email(RuleFor(request => request.Email));
        CredentialRules.NewPassword(RuleFor(request => request.Password));
        RuleFor(request => request.DisplayName).MustBeDisplayName();
        RuleFor(request => request.PreferredLanguage).MustBeSupportedLanguage();
        RuleFor(request => request.TimeZoneId).MustBeTimeZone();
        RuleFor(request => request.CountryCode).MustBeCountryCode();
        RuleFor(request => request.CurriculumId).GreaterThan(0);
        RuleFor(request => request.GradeLevelId).GreaterThan(0);
        RuleFor(request => request.SubjectIds).NotNull()
            .Must(ids => ids.Count <= CredentialRules.MaxSubjects && ids.Distinct().Count() == ids.Count)
            .WithMessage($"Choose up to {CredentialRules.MaxSubjects} different subjects.");
    }
}

internal sealed class RegisterTeacherRequestValidator : AbstractValidator<RegisterTeacherRequest>
{
    public RegisterTeacherRequestValidator()
    {
        CredentialRules.Email(RuleFor(request => request.Email));
        CredentialRules.NewPassword(RuleFor(request => request.Password));
        RuleFor(request => request.DisplayName).MustBeDisplayName();
        RuleFor(request => request.PreferredLanguage).MustBeSupportedLanguage();
        RuleFor(request => request.TimeZoneId).MustBeTimeZone();
        RuleFor(request => request.CountryCode).MustBeCountryCode();
        RuleFor(request => request.Headline).NotEmpty().Length(3, 150);
    }
}

internal sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().MaximumLength(CredentialRules.MaxEmailLength);
        RuleFor(request => request.Password).NotEmpty().MaximumLength(CredentialRules.MaxPasswordLength);
    }
}

internal sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(request => request.DisplayName).MustBeDisplayName();
        RuleFor(request => request.PreferredLanguage).MustBeSupportedLanguage();
        RuleFor(request => request.TimeZoneId).MustBeTimeZone();
        RuleFor(request => request.CountryCode).MustBeCountryCode();
    }
}

internal sealed class UpdateStudentStudyRequestValidator : AbstractValidator<UpdateStudentStudyRequest>
{
    public UpdateStudentStudyRequestValidator()
    {
        RuleFor(request => request.CurriculumId).GreaterThan(0);
        RuleFor(request => request.GradeLevelId).GreaterThan(0);
        RuleFor(request => request.SubjectIds).NotNull()
            .Must(ids => ids.Count <= CredentialRules.MaxSubjects && ids.Distinct().Count() == ids.Count)
            .WithMessage($"Choose up to {CredentialRules.MaxSubjects} different subjects.");
    }
}

internal sealed class UpdateTeacherProfileRequestValidator : AbstractValidator<UpdateTeacherProfileRequest>
{
    public UpdateTeacherProfileRequestValidator()
    {
        RuleFor(request => request.Headline).NotEmpty().Length(3, 150);
        RuleFor(request => request.Bio).NotNull().MaximumLength(2000);
    }
}
