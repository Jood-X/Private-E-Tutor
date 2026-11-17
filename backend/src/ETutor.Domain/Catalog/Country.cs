using ETutor.Domain.Common;

namespace ETutor.Domain.Catalog;

public sealed class Country
{
    public int Id { get; set; }

    /// <summary>ISO 3166-1 alpha-2 code, upper case (e.g. "JO").</summary>
    public required string IsoCode { get; set; }

    public required LocalizedText Name { get; set; }

    /// <summary>IANA time-zone ID used as the default for users in this country.</summary>
    public required string DefaultTimeZoneId { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public ICollection<CurriculumAvailability> CurriculumAvailabilities { get; } = [];
}
