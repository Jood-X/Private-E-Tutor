using ETutor.Api.Common;
using ETutor.Api.ErrorHandling;
using ETutor.Application.Common;
using Microsoft.Extensions.Options;
using Serilog;

namespace ETutor.Api.Configuration;

internal static class ApplicationBuilderExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages(WriteEmptyErrorResponseAsync);
        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (context, _, exception) => exception is not null || context.Response.StatusCode >= 500
                ? Serilog.Events.LogEventLevel.Error
                : Serilog.Events.LogEventLevel.Information;
            options.EnrichDiagnosticContext = (diagnostics, context) =>
                diagnostics.Set("RouteTemplate", (context.GetEndpoint() as RouteEndpoint)?.RoutePattern.RawText ?? "(none)");
        });
        app.Use(AddResponseHeadersAsync);

        if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseRequestLocalization();
        app.UseCors(ServiceCollectionExtensions.CorsPolicyName);
        app.UseRateLimiter();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health");

        if (app.Environment.IsDevelopment())
        {
            var productName = app.Services.GetRequiredService<IOptions<BrandingOptions>>().Value.ProductName;
            app.MapOpenApi();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/openapi/v1.json", $"{productName} API v1");
                options.DocumentTitle = $"{productName} API";
            });
        }

        return app;
    }

    /// <summary>Adds the trace ID and baseline security headers to every response.</summary>
    private static Task AddResponseHeadersAsync(HttpContext context, Func<Task> next)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers[HttpContextExtensions.TraceIdHeader] = context.GetTraceId();
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            return Task.CompletedTask;
        });
        return next();
    }

    /// <summary>Gives bodiless 4xx/5xx responses (unknown route, wrong method, …) the standard envelope.</summary>
    private static Task WriteEmptyErrorResponseAsync(Microsoft.AspNetCore.Diagnostics.StatusCodeContext statusContext)
    {
        var context = statusContext.HttpContext;
        var (code, message) = context.Response.StatusCode switch
        {
            StatusCodes.Status404NotFound => (ErrorCodes.Common.NotFound, "The requested resource was not found."),
            StatusCodes.Status405MethodNotAllowed => (ErrorCodes.Common.MethodNotAllowed, "This method is not allowed here."),
            StatusCodes.Status401Unauthorized => (ErrorCodes.Common.Unauthorized, "Authentication is required."),
            StatusCodes.Status403Forbidden => (ErrorCodes.Common.Forbidden, "You do not have access to this resource."),
            >= 500 => (ErrorCodes.Common.Unexpected, "An unexpected error occurred. Please try again later."),
            _ => (ErrorCodes.Common.BadRequest, "The request could not be processed."),
        };
        return ApiErrorWriter.WriteAsync(context, context.Response.StatusCode, code, message);
    }
}
