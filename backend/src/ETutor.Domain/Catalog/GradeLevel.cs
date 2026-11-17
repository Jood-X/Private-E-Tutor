using ETutor.Domain.Common;

namespace ETutor.Domain.Catalog;

/// <summary>Where a student is within a curriculum (e.g. "Grade 12"). Not a calendar year.</summary>
public sealed class GradeLevel
{
    public int Id { get; set; }

    public int CurriculumId { get; set; }

    public required string Code { get; set; }

    public required LocalizedText Name { get; set; }

    /// <summary>Position within the curriculum; also used for ordering.</summary>
    public byte Ordinal { get; set; }

    public Curriculum? Curriculum { get; set; }

    public ICollection<GradeSubject> Subjects { get; } = [];
}
