namespace HuGuWeb.TechnicalService.Domain;

public interface ITechnicalServiceClock
{
    DateTimeOffset UtcNow { get; }
}
