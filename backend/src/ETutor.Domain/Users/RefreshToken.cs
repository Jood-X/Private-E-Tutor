namespace ETutor.Domain.Users;

/// <summary>
/// A long-lived credential that can be exchanged once for new tokens. Only a SHA-256 hash of the token
/// is stored. Tokens issued from the same sign-in share a <see cref="FamilyId"/>; presenting a token that
/// was already used revokes the whole family (reuse detection).
/// </summary>
public sealed class RefreshToken
{
    public const int HashLength = 32;

    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public required byte[] TokenHash { get; set; }

    public Guid FamilyId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>Set when the token is used (rotated) or revoked; null while usable.</summary>
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>The token issued in exchange for this one, when it was rotated.</summary>
    public Guid? ReplacedByTokenId { get; set; }

    public bool IsUsable(DateTime utcNow) => RevokedAtUtc is null && ExpiresAtUtc > utcNow;
}
