namespace ETutor.Domain.Common;

/// <summary>Entities whose creation and last-update instants (UTC) are stamped automatically on save.</summary>
public interface IAuditable
{
    DateTime CreatedAtUtc { get; set; }

    DateTime UpdatedAtUtc { get; set; }
}
