using HuGuWeb.RoomOperations.Application;
using HuGuWeb.RoomOperations.Domain;

namespace HuGuWeb.UnitTests.RoomOperations;

internal sealed class FakeRoomClock : IRoomOperationsClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 8, 23, 10, 0, 0, TimeSpan.Zero);

    public void Advance(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
}

internal sealed class FixedRoomWorkplace(Guid propertyId) : IRoomOperationsWorkplace
{
    public Guid PropertyId { get; } = propertyId;
    public bool IsConfigured => true;
}

internal sealed class InMemoryAssignableEmployees : IAssignableEmployeeDirectory
{
    public List<AssignableEmployee> Assignable { get; } = [];
    public List<AssignableEmployee> Known { get; } = [];

    public Task<AssignableEmployee?> FindAssignableAsync(Guid employeeId, CancellationToken cancellationToken) =>
        Task.FromResult(Assignable.FirstOrDefault(item => item.EmployeeId == employeeId));

    public Task<IReadOnlyList<AssignableEmployee>> ListAssignableAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AssignableEmployee>>(Assignable.ToArray());

    public Task<IReadOnlyDictionary<Guid, AssignableEmployee>> GetEmployeesAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        var names = Known.Concat(Assignable)
            .Where(item => employeeIds.Contains(item.EmployeeId))
            .GroupBy(item => item.EmployeeId)
            .ToDictionary(group => group.Key, group => group.First());
        return Task.FromResult<IReadOnlyDictionary<Guid, AssignableEmployee>>(names);
    }
}

internal sealed class InMemoryRoomOperationsStore : IRoomOperationsStore
{
    public List<Room> Rooms { get; } = [];
    public List<HousekeepingWorkItem> WorkItems { get; } = [];
    public List<RoomReadinessHistoryEntry> History { get; } = [];
    public List<RoomInspection> Inspections { get; } = [];

    public Task<Room?> GetRoomAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Rooms.FirstOrDefault(item => item.Id == id));

    public Task<IReadOnlyList<Room>> ListRoomsAsync(Guid propertyId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Room>>(Rooms.Where(item => item.PropertyId == propertyId).ToArray());

    public Task<HousekeepingWorkItem?> GetWorkItemAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(WorkItems.FirstOrDefault(item => item.Id == id));

    public Task<HousekeepingWorkItem?> FindOpenWorkAsync(Guid roomId, CancellationToken cancellationToken) =>
        Task.FromResult(WorkItems.FirstOrDefault(item => item.RoomId == roomId && item.IsOpen));

    public Task<IReadOnlyList<HousekeepingWorkItem>> ListWorkItemsAsync(
        Guid roomId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<HousekeepingWorkItem>>(WorkItems.Where(item => item.RoomId == roomId).ToArray());

    public Task<IReadOnlyList<HousekeepingWorkItem>> ListWorkItemsForRoomsAsync(
        IReadOnlyCollection<Guid> roomIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<HousekeepingWorkItem>>(
            WorkItems.Where(item => roomIds.Contains(item.RoomId)).ToArray());

    public Task<IReadOnlyList<RoomReadinessHistoryEntry>> ListHistoryAsync(
        Guid roomId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RoomReadinessHistoryEntry>>(
            History.Where(item => item.RoomId == roomId).ToArray());

    public Task<IReadOnlyList<RoomInspection>> ListInspectionsAsync(
        Guid roomId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RoomInspection>>(Inspections.Where(item => item.RoomId == roomId).ToArray());

    public void AddRoom(Room room) => Rooms.Add(room);

    public void AddWorkItem(HousekeepingWorkItem workItem) => WorkItems.Add(workItem);

    public void AddHistory(RoomReadinessHistoryEntry entry) => History.Add(entry);

    public void AddInspection(RoomInspection inspection) => Inspections.Add(inspection);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class RoomOperationsHarness
{
    public Guid PropertyId { get; } = Guid.CreateVersion7();
    public Guid OtherPropertyId { get; } = Guid.CreateVersion7();
    public Guid RoomId { get; } = Guid.CreateVersion7();
    public Guid EmployeeId { get; } = Guid.CreateVersion7();
    public Guid OtherEmployeeId { get; } = Guid.CreateVersion7();
    public Guid ActorUserId { get; } = Guid.CreateVersion7();
    public Guid InspectorUserId { get; } = Guid.CreateVersion7();

    public FakeRoomClock Clock { get; } = new();
    public InMemoryRoomOperationsStore Store { get; } = new();
    public InMemoryAssignableEmployees Employees { get; } = new();
    public FixedRoomWorkplace Workplace { get; }

    public RequestNeedsCleaningUseCase NeedsCleaning { get; }
    public CompleteCleaningUseCase CompleteCleaning { get; }
    public InspectRoomUseCase Inspect { get; }
    public ListRoomOperationsQuery List { get; }
    public GetRoomOperationsDetailQuery Detail { get; }

    public RoomOperationsHarness()
    {
        Workplace = new FixedRoomWorkplace(PropertyId);
        Employees.Assignable.Add(new AssignableEmployee(EmployeeId, "Ayşe", "Yılmaz", "P-1001"));
        Employees.Assignable.Add(new AssignableEmployee(OtherEmployeeId, "Mehmet", "Kaya", "P-1002"));
        Employees.Known.AddRange(Employees.Assignable);

        NeedsCleaning = new RequestNeedsCleaningUseCase(Store, Employees, Workplace, Clock);
        CompleteCleaning = new CompleteCleaningUseCase(Store, Employees, Workplace, Clock);
        Inspect = new InspectRoomUseCase(Store, Employees, Workplace, Clock);
        List = new ListRoomOperationsQuery(Store, Employees, Workplace);
        Detail = new GetRoomOperationsDetailQuery(Store, Employees, Workplace);

        SeedRoom(RoomId, "101");
    }

    public Room SeedRoom(Guid id, string number, Guid? propertyId = null)
    {
        Assert.True(Room.TryCreate(id, propertyId ?? PropertyId, number, Guid.CreateVersion7(), out var room, out _));
        Store.Rooms.Add(room!);
        Store.History.Add(RoomReadinessHistoryEntry.Record(
            Guid.CreateVersion7(),
            room!.Id,
            room.ReadinessCycleId,
            RoomReadiness.Dirty,
            ReadinessChangeCause.Seeded,
            Clock.UtcNow));
        return room;
    }

    public RequestNeedsCleaningCommand NeedsCleaningCommand(
        Guid? roomId = null,
        Guid? employeeId = null,
        TaskPriority priority = TaskPriority.Normal) =>
        new(roomId ?? RoomId, employeeId ?? EmployeeId, priority, ActorUserId);
}
