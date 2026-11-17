using ETutor.Application.Abstractions;

namespace ETutor.Infrastructure;

internal sealed class SystemClock(TimeProvider timeProvider) : IClock
{
    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;
}
