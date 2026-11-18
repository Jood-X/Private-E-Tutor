using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using ETutor.Api.ErrorHandling;
using ETutor.Api.Localization;
using ETutor.Api.Logging;
using ETutor.Api.Security;
using ETutor.Application.Abstractions;
using ETutor.Application.Common;
using ETutor.Domain.Common;
using ETutor.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ETutor.Api.Configuration;

internal static class ServiceCollectionExtensions
{
    public const string CorsPolicyName = "AllowedClients";
    public const string AuthRateLimitPolicy = "auth";

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<BrandingOptions>().Bind(configuration.GetSection(BrandingOptions.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<RateLimitingOptions>().Bind(configuration.GetSection(RateLimitingOptions.SectionName))
            .ValidateDataAnnotations().ValidateOnStart();

        services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = ValidationResponseFactory.Create);

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddScoped<IRequestLanguage, RequestLanguage>();

        services.AddRequestLocalization(options =>
        {
            var cultures = LanguageCodes.Supported.Select(code => new CultureInfo(code)).ToList();
            options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(LanguageCodes.Default);
            options.SupportedCultures = cultures;
            options.SupportedUICultures = cultures;
        });

        services.AddApiCors(configuration);
        services.AddApiRateLimiting();
        services.AddApiAuthentication();
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, _) =>
            {
                var branding = context.ApplicationServices.GetRequiredService<IOptions<BrandingOptions>>().Value;
                document.Info.Title = $"{branding.ProductName} API";
                document.Info.Version = "v1";
                document.Info.Description =
                    "Every JSON response uses the envelope { success, message, data, errors, traceId }. " +
                    "Errors carry stable codes for client localization. Send Accept-Language: en | ar.";
                return Task.CompletedTask;
            });
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
        });
        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");
        return services;
    }

    private static void AddApiCors(this IServiceCollection services, IConfiguration configuration)
    {
        var corsOptions = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();
        services.AddCors(options => options.AddPolicy(CorsPolicyName, policy =>
        {
            if (corsOptions.AllowedOrigins.Count > 0)
            {
                policy.WithOrigins([.. corsOptions.AllowedOrigins]).AllowAnyHeader().AllowAnyMethod()
                    .WithExposedHeaders(Common.HttpContextExtensions.TraceIdHeader);
            }
        }));
    }

    private static void AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var limits = context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

                // Partition by client IP. Behind a reverse proxy, forwarded headers must be configured
                // first (see docs/security.md), otherwise every client shares the proxy's address.
                var clientKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(clientKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limits.PermitLimit,
                    Window = TimeSpan.FromSeconds(limits.WindowSeconds),
                    QueueLimit = 0,
                });
            });

            // Stricter limit for credential endpoints (sign-in, registration, refresh), applied in
            // addition to the global one, to slow down password guessing and account creation abuse.
            options.AddPolicy(AuthRateLimitPolicy, context =>
            {
                var limits = context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
                var clientKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(clientKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limits.AuthPermitLimit,
                    Window = TimeSpan.FromSeconds(limits.WindowSeconds),
                    QueueLimit = 0,
                });
            });
            options.OnRejected = async (rejection, cancellationToken) =>
            {
                var context = rejection.HttpContext;
                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("RateLimiting");
                LogMessages.RateLimited(logger, context.Request.Method, context.Request.Path);
                if (rejection.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                await ApiErrorWriter.WriteAsync(
                    context, StatusCodes.Status429TooManyRequests, ErrorCodes.Common.RateLimited,
                    "Too many requests. Please wait and try again.");
            };
        });
    }
}
