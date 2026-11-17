using ETutor.Application.Common.Results;

namespace ETutor.Api.Common;

/// <summary>The single mapping from business error categories to HTTP status codes.</summary>
public static class ErrorStatusCodes
{
    public static int For(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unmapped error type."),
    };
}
