namespace ETutor.Domain.Catalog;

/// <summary>A calendar academic year of a curriculum (e.g. "2026/2027"). Calendars differ per curriculum.</summary>
public sealed class AcademicYear
{
    public int Id { get; set; }

    public int CurriculumId { get; set; }

    public required string Label { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public Curriculum? Curriculum { get; set; }
}
