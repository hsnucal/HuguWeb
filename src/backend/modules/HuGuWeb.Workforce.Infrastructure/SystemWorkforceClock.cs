using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Infrastructure;

public sealed class SystemWorkforceClock(TimeProvider time) : IWorkforceClock
{
    public DateOnly Today => DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
    public DateTimeOffset UtcNow => time.GetUtcNow();
}
