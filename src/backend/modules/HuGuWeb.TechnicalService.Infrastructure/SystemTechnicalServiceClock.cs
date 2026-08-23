using HuGuWeb.TechnicalService.Domain;

namespace HuGuWeb.TechnicalService.Infrastructure;

public sealed class SystemTechnicalServiceClock : ITechnicalServiceClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
