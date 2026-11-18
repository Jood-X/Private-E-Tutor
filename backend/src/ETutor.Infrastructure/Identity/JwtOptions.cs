using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ETutor.Infrastructure.Identity;

/// <summary>
/// Token settings. <see cref="SigningKey"/> is a secret: set it with `dotnet user-secrets` locally
/// and the `Jwt__SigningKey` environment variable on servers. Never commit it.
/// </summary>
public sealed class JwtOptions : IValidatableObject
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 needs at least 256 bits of key material.</summary>
    public const int MinimumKeyBytes = 32;

    /// <summary>Claim type that carries role names in access tokens.</summary>
    public const string RoleClaimType = "role";

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    public string SigningKey { get; init; } = string.Empty;

    [Range(1, 60)]
    public int AccessTokenMinutes { get; init; } = 15;

    [Range(1, 90)]
    public int RefreshTokenDays { get; init; } = 30;

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Encoding.UTF8.GetByteCount(SigningKey) < MinimumKeyBytes)
        {
            yield return new ValidationResult(
                $"Jwt:SigningKey must be at least {MinimumKeyBytes} bytes. Run scripts/setup.ps1 or see docs/setup.md.",
                [nameof(SigningKey)]);
        }
    }
}
