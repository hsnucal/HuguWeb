using HuGuWeb.RoomOperations.Application;
using HuGuWeb.RoomOperations.Domain;
using Microsoft.EntityFrameworkCore;

namespace HuGuWeb.RoomOperations.Infrastructure.Persistence;

public sealed class EfRoomOperationsStore(RoomOperationsDbContext dbContext) : IRoomOperationsStore
{
    public Task<Room?> GetRoomAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Rooms.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Room>> ListRoomsAsync(Guid propertyId, CancellationToken cancellationToken) =>
        await dbContext.Rooms
            .Where(item => item.PropertyId == propertyId)
            .ToArrayAsync(cancellationToken);

    public Task<HousekeepingWorkItem?> GetWorkItemAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.HousekeepingWorkItems.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

    public Task<HousekeepingWorkItem?> FindOpenWorkAsync(Guid roomId, CancellationToken cancellationToken) =>
        dbContext.HousekeepingWorkItems.FirstOrDefaultAsync(
            item => item.RoomId == roomId && item.State == HousekeepingWorkState.Open,
            cancellationToken);

    public async Task<IReadOnlyList<HousekeepingWorkItem>> ListWorkItemsAsync(
        Guid roomId,
        CancellationToken cancellationToken) =>
        await dbContext.HousekeepingWorkItems
            .Where(item => item.RoomId == roomId)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<HousekeepingWorkItem>> ListWorkItemsForRoomsAsync(
        IReadOnlyCollection<Guid> roomIds,
        CancellationToken cancellationToken) =>
        await dbContext.HousekeepingWorkItems
            .Where(item => roomIds.Contains(item.RoomId))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<RoomReadinessHistoryEntry>> ListHistoryAsync(
        Guid roomId,
        CancellationToken cancellationToken) =>
        await dbContext.RoomReadinessHistory
            .Where(item => item.RoomId == roomId)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<RoomInspection>> ListInspectionsAsync(
        Guid roomId,
        CancellationToken cancellationToken) =>
        await dbContext.RoomInspections
            .Where(item => item.RoomId == roomId)
            .ToArrayAsync(cancellationToken);

    public void AddRoom(Room room) => dbContext.Rooms.Add(room);

    public void AddWorkItem(HousekeepingWorkItem workItem) => dbContext.HousekeepingWorkItems.Add(workItem);

    public void AddHistory(RoomReadinessHistoryEntry entry) => dbContext.RoomReadinessHistory.Add(entry);

    public void AddInspection(RoomInspection inspection) => dbContext.RoomInspections.Add(inspection);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
