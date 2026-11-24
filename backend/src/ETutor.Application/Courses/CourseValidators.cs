using ETutor.Application.Common;
using ETutor.Domain.Common;
using ETutor.Domain.Content;
using FluentValidation;

namespace ETutor.Application.Courses;

internal static class ContentLimits
{
    public const int TitleMax = 200;
    public const int ShortTextMax = 2000;
    public const int DescriptionMax = 4000;
    public const int NotesMax = 100_000;
    public const int UrlMax = 500;
    public const short MaxMinutes = 600;
}

internal sealed class CreateCourseRequestValidator : AbstractValidator<CreateCourseRequest>
{
    public CreateCourseRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(ContentLimits.TitleMax);
        RuleFor(request => request.Description).NotNull().MaximumLength(ContentLimits.DescriptionMax);
        RuleFor(request => request.ContentLanguage).Must(LanguageCodes.IsSupported).WithMessage("Language must be en or ar.");
        RuleFor(request => request.CurriculumId).GreaterThan(0);
        RuleFor(request => request.GradeLevelId).GreaterThan(0);
        RuleFor(request => request.SubjectId).GreaterThan(0);
        RuleFor(request => request.AcademicYearId).GreaterThan(0);
    }
}

internal sealed class UpdateCourseRequestValidator : AbstractValidator<UpdateCourseRequest>
{
    public UpdateCourseRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(ContentLimits.TitleMax);
        RuleFor(request => request.Description).NotNull().MaximumLength(ContentLimits.DescriptionMax);
        RuleFor(request => request.ContentLanguage).Must(LanguageCodes.IsSupported).WithMessage("Language must be en or ar.");
    }
}

internal sealed class UnitRequestValidator : AbstractValidator<UnitRequest>
{
    public UnitRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(ContentLimits.TitleMax);
        RuleFor(request => request.Description).NotNull().MaximumLength(ContentLimits.ShortTextMax);
    }
}

internal sealed class LessonRequestValidator : AbstractValidator<LessonRequest>
{
    public LessonRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(ContentLimits.TitleMax);
        RuleFor(request => request.Summary).NotNull().MaximumLength(ContentLimits.ShortTextMax);
        RuleFor(request => request.Notes).NotNull().MaximumLength(ContentLimits.NotesMax);
        RuleFor(request => request.EstimatedMinutes).InclusiveBetween((short)1, ContentLimits.MaxMinutes)
            .When(request => request.EstimatedMinutes.HasValue);
    }
}

internal sealed class LinkResourceRequestValidator : AbstractValidator<LinkResourceRequest>
{
    public LinkResourceRequestValidator()
    {
        RuleFor(request => request.Title).NotEmpty().MaximumLength(ContentLimits.TitleMax);
        RuleFor(request => request.Url).NotEmpty().MaximumLength(ContentLimits.UrlMax)
            .Must(ContentRules.IsAllowedLink)
            .WithErrorCode(ErrorCodes.Course.InvalidLink)
            .WithMessage("Links must be full https:// addresses.");
    }
}
