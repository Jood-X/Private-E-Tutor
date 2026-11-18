using ETutor.Application.Common.Results;
using ETutor.Domain.Common;
using FluentValidation;
using FluentValidation.Results;

namespace ETutor.Application.Common.Validation;

public static class ValidationExtensions
{
    /// <summary>
    /// Converts FluentValidation failures into result errors. Rules that set an explicit error code
    /// ("area.reason") keep it; built-in rules map to <c>common.validation_failed</c>.
    /// </summary>
    public static IReadOnlyList<Error> ToErrors(this ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Errors
            .Select(failure => Error.Validation(
                failure.ErrorCode.Contains('.', StringComparison.Ordinal) ? failure.ErrorCode : ErrorCodes.Common.ValidationFailed,
                failure.ErrorMessage,
                ToCamelCase(failure.PropertyName)))
            .ToList();
    }

    /// <summary>Validates <paramref name="request"/>; returns the errors, or null when it is valid.</summary>
    public static async Task<IReadOnlyList<Error>?> ValidateToErrorsAsync<T>(
        this IValidator<T> validator, T request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(validator);
        var result = await validator.ValidateAsync(request, cancellationToken);
        return result.IsValid ? null : result.ToErrors();
    }

    public static IRuleBuilderOptions<T, string> MustBeSupportedLanguage<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(LanguageCodes.IsSupported).WithMessage("Language must be one of: en, ar.");

    public static IRuleBuilderOptions<T, string> MustBeTimeZone<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MaximumLength(64)
            .Must(id => TimeZoneInfo.TryFindSystemTimeZoneById(id, out _))
            .WithMessage("Time zone must be a valid IANA ID such as Asia/Amman.");

    public static IRuleBuilderOptions<T, string> MustBeCountryCode<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .Length(2)
            .Must(code => code.All(char.IsAsciiLetter))
            .WithMessage("Country must be a two-letter ISO code.");

    public static IRuleBuilderOptions<T, string> MustBeDisplayName<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .Must(name => name.Trim().Length is >= 2 and <= 100)
            .WithMessage("Name must be between 2 and 100 characters.");

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
