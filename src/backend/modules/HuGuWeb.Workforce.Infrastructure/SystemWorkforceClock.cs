using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Infrastructure;

public sealed class SystemWorkforceClock : IWorkforceClock
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
