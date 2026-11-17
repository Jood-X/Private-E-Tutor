using ETutor.Domain.Common;

namespace ETutor.Domain.Catalog;

/// <summary>
/// An educational system (e.g. Jordanian Tawjihi, Cambridge). Not owned by a country:
/// <see cref="CurriculumAvailability"/> lists the countries where it is offered.
/// </summary>
public sealed class Curriculum
{
    public int Id { get; set; }

    public required string Code { get; set; }

    public required LocalizedText Name { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public ICollection<CurriculumAvailability> Availabilities { get; } = [];

    public ICollection<CurriculumStream> Streams { get; } = [];

    public ICollection<GradeLevel> GradeLevels { get; } = [];

    public ICollection<AcademicYear> AcademicYears { get; } = [];
}
