namespace ETutor.Application.Abstractions;

/// <summary>The only source of "now" in business code, so time-dependent rules are testable.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
