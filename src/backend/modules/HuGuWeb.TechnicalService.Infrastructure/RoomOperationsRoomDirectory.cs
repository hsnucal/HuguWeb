using HuGuWeb.RoomOperations.Application;
using HuGuWeb.TechnicalService.Application;

namespace HuGuWeb.TechnicalService.Infrastructure;

public sealed class RoomOperationsRoomDirectory(IRoomOperationsStore store) : IRoomIdentityDirectory
{
    public async Task<KnownRoom?> FindAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var room = await store.GetRoomAsync(roomId, cancellationToken);
        return room is null ? null : new KnownRoom(room.Id, room.PropertyId, room.Number, room.IsActive);
    }

    public async Task<IReadOnlyList<KnownRoom>> ListActiveAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        var rooms = await store.ListRoomsAsync(propertyId, cancellationToken);
        return rooms
            .Where(room => room.IsActive)
            .Select(room => new KnownRoom(room.Id, room.PropertyId, room.Number, room.IsActive))
            .ToArray();
    }

    public async Task<IReadOnlyDictionary<Guid, KnownRoom>> GetAsync(
        IReadOnlyCollection<Guid> roomIds,
        CancellationToken cancellationToken)
    {
        if (roomIds.Count == 0)
        {
            return new Dictionary<Guid, KnownRoom>();
        }

        var result = new Dictionary<Guid, KnownRoom>();
        foreach (var id in roomIds.Distinct())
        {
            var room = await store.GetRoomAsync(id, cancellationToken);
            if (room is not null)
            {
                result[room.Id] = new KnownRoom(room.Id, room.PropertyId, room.Number, room.IsActive);
            }
        }

        return result;
    }
}
