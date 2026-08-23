using HuGuWeb.RoomOperations.Domain;

namespace HuGuWeb.RoomOperations.Application;

public interface IRoomOperationsWorkplace
{
    Guid PropertyId { get; }
    bool IsConfigured { get; }
}

public interface IAssignableEmployeeDirectory
{
    Task<AssignableEmployee?> FindAssignableAsync(Guid employeeId, CancellationToken cancellationToken);
    Task<IReadOnlyList<AssignableEmployee>> ListAssignableAsync(CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, AssignableEmployee>> GetEmployeesAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken);
}

public sealed record AssignableEmployee(
    Guid EmployeeId,
    string GivenName,
    string FamilyName,
    string PersonnelNumber);

public interface IRoomOperationsStore
{
    Task<Room?> GetRoomAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Room>> ListRoomsAsync(Guid propertyId, CancellationToken cancellationToken);
    Task<HousekeepingWorkItem?> GetWorkItemAsync(Guid id, CancellationToken cancellationToken);
    Task<HousekeepingWorkItem?> FindOpenWorkAsync(Guid roomId, CancellationToken cancellationToken);
    Task<IReadOnlyList<HousekeepingWorkItem>> ListWorkItemsAsync(Guid roomId, CancellationToken cancellationToken);
    Task<IReadOnlyList<HousekeepingWorkItem>> ListWorkItemsForRoomsAsync(
        IReadOnlyCollection<Guid> roomIds,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<RoomReadinessHistoryEntry>> ListHistoryAsync(Guid roomId, CancellationToken cancellationToken);
    Task<IReadOnlyList<RoomInspection>> ListInspectionsAsync(Guid roomId, CancellationToken cancellationToken);

    void AddRoom(Room room);
    void AddWorkItem(HousekeepingWorkItem workItem);
    void AddHistory(RoomReadinessHistoryEntry entry);
    void AddInspection(RoomInspection inspection);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
