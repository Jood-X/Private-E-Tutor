using System.ComponentModel.DataAnnotations;

namespace ETutor.Api.Configuration;

/// <summary>User-visible product branding. The product name is provisional, so it lives only in configuration.</summary>
public sealed class BrandingOptions
{
    public const string SectionName = "Branding";

    [Required]
    public string ProductName { get; init; } = string.Empty;
}

/// <summary>Origins allowed to call the API from a browser. Native mobile apps are unaffected by CORS.</summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    public IReadOnlyList<string> AllowedOrigins { get; init; } = [];
}

/// <summary>Per-client fixed-window limit applied to every endpoint.</summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    [Range(1, 100_000)]
    public int PermitLimit { get; init; }

    [Range(1, 3600)]
    public int WindowSeconds { get; init; }
}
