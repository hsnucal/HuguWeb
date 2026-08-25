using HuGuWeb.RoomOperations.Domain;

namespace HuGuWeb.RoomOperations.Infrastructure;

public sealed class SystemRoomOperationsClock(TimeProvider time) : IRoomOperationsClock
{
    public DateTimeOffset UtcNow => time.GetUtcNow();
}
