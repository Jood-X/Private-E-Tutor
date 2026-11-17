using ETutor.Api.Logging;
using ETutor.Application.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace ETutor.Api.ErrorHandling;

/// <summary>
/// Last line of defense: logs the exception with full detail and returns a generic envelope.
/// Exception messages, stack traces and SQL are never sent to the client.
/// </summary>
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is BadHttpRequestException badRequest)
        {
            LogMessages.BadRequest(logger, badRequest.Message);
            await ApiErrorWriter.WriteAsync(
                httpContext, badRequest.StatusCode, ErrorCodes.Common.BadRequest, "The request could not be read.");
            return true;
        }

        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            // The client disconnected; nobody is waiting for a response.
            return true;
        }

        LogMessages.UnhandledException(logger, exception, httpContext.Request.Method, httpContext.Request.Path);
        await ApiErrorWriter.WriteAsync(
            httpContext,
            StatusCodes.Status500InternalServerError,
            ErrorCodes.Common.Unexpected,
            "An unexpected error occurred. Please try again later.");
        return true;
    }
}
