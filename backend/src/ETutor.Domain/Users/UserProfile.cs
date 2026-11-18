using ETutor.Domain.Catalog;
using ETutor.Domain.Common;

namespace ETutor.Domain.Users;

/// <summary>
/// Data every user has, independent of role. Shares its ID with the identity account
/// (credentials and roles live in the identity store, not here).
/// </summary>
public sealed class UserProfile : IAuditable
{
    public Guid Id { get; set; }

    public required string DisplayName { get; set; }

    /// <summary>ISO 639-1 code of the user's UI language (see <see cref="LanguageCodes"/>).</summary>
    public required string PreferredLanguage { get; set; }

    /// <summary>IANA time-zone ID used to display dates and lesson times.</summary>
    public required string TimeZoneId { get; set; }

    public int CountryId { get; set; }

    public Country? Country { get; set; }

    public TeacherProfile? Teacher { get; set; }

    public StudentProfile? Student { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
