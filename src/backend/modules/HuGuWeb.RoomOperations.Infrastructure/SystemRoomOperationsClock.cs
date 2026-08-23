using HuGuWeb.RoomOperations.Domain;

namespace HuGuWeb.RoomOperations.Infrastructure;

public sealed class SystemRoomOperationsClock : IRoomOperationsClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
