using ETutor.Domain.Common;

namespace ETutor.Domain.Catalog;

/// <summary>An optional specialization within a curriculum (e.g. Tawjihi Scientific).</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming", "CA1711:Identifiers should not have incorrect suffix",
    Justification = "\"Stream\" is the education term (e.g. Tawjihi Scientific stream), not an I/O stream.")]
public sealed class CurriculumStream
{
    public int Id { get; set; }

    public int CurriculumId { get; set; }

    public required string Code { get; set; }

    public required LocalizedText Name { get; set; }

    public int SortOrder { get; set; }

    public Curriculum? Curriculum { get; set; }
}
