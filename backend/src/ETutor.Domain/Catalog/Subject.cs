using ETutor.Domain.Common;

namespace ETutor.Domain.Catalog;

/// <summary>A global subject (e.g. physics), shared across curricula for discovery and analytics.</summary>
public sealed class Subject
{
    public int Id { get; set; }

    public required string Code { get; set; }

    public required LocalizedText Name { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<GradeSubject> GradeLevels { get; } = [];
}
