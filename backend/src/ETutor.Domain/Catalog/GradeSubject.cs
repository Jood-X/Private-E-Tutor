namespace ETutor.Domain.Catalog;

/// <summary>Join entity: a subject that is taught at a grade level.</summary>
public sealed class GradeSubject
{
    public int GradeLevelId { get; set; }

    public int SubjectId { get; set; }

    public GradeLevel? GradeLevel { get; set; }

    public Subject? Subject { get; set; }
}
