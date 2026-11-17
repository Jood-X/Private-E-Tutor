namespace ETutor.Domain.Catalog;

/// <summary>Join entity: a curriculum offered in a country.</summary>
public sealed class CurriculumAvailability
{
    public int CountryId { get; set; }

    public int CurriculumId { get; set; }

    public Country? Country { get; set; }

    public Curriculum? Curriculum { get; set; }
}
