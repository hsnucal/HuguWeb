namespace HuGuWeb.RoomOperations.Domain;

public interface IRoomOperationsClock
{
    DateTimeOffset UtcNow { get; }
}
