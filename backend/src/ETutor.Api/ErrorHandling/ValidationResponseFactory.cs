using ETutor.Api.Common;
using ETutor.Api.Logging;
using ETutor.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace ETutor.Api.ErrorHandling;

/// <summary>Turns model-binding/validation failures into the standard 400 envelope.</summary>
internal static class ValidationResponseFactory
{
    public static IActionResult Create(ActionContext context)
    {
        var errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .SelectMany(entry => entry.Value!.Errors.Select(error => new ApiError(
                ErrorCodes.Common.ValidationFailed,
                string.IsNullOrWhiteSpace(error.ErrorMessage) ? "The value is invalid." : error.ErrorMessage,
                ToCamelCase(entry.Key))))
            .ToList();

        if (errors.Count == 0)
        {
            errors.Add(new ApiError(ErrorCodes.Common.ValidationFailed, "The request is invalid."));
        }

        var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(ValidationResponseFactory));
        if (logger.IsEnabled(LogLevel.Information))
        {
            var fields = string.Join(", ", errors.Select(error => error.Field).Distinct());
            LogMessages.ValidationFailed(logger, fields);
        }

        return new BadRequestObjectResult(ApiResponse.Fail<object>(errors, context.HttpContext.GetTraceId()));
    }

    private static string ToCamelCase(string key) =>
        string.IsNullOrEmpty(key) ? key : char.ToLowerInvariant(key[0]) + key[1..];
}
