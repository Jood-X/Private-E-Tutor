using ETutor.Api.Common;

namespace ETutor.Api.ErrorHandling;

/// <summary>Writes the standard error envelope from middleware (outside of MVC).</summary>
internal static class ApiErrorWriter
{
    public static Task WriteAsync(HttpContext context, int statusCode, string code, string message)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(
            ApiResponse.Fail(code, message, context.GetTraceId()),
            context.RequestAborted);
    }
}
