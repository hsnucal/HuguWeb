namespace HuGuWeb.Workforce.Domain;

public interface IWorkforceClock
{
    DateOnly Today { get; }
    DateTimeOffset UtcNow { get; }
}
