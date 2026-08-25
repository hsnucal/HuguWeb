using HuGuWeb.TechnicalService.Domain;

namespace HuGuWeb.TechnicalService.Infrastructure;

public sealed class SystemTechnicalServiceClock(TimeProvider time) : ITechnicalServiceClock
{
    public DateTimeOffset UtcNow => time.GetUtcNow();
}
