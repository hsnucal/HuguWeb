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

internal sealed class InMemoryRoomServiceabilityLookup : IRoomServiceabilityLookup
{
    public IRoomServiceabilityLookup? Inner { get; set; }
    public Dictionary<Guid, RoomServiceabilitySnapshot> Snapshots { get; } = [];

    public Task<IReadOnlyDictionary<Guid, RoomServiceabilitySnapshot>> GetForRoomsAsync(
        Guid propertyId,
        IReadOnlyCollection<Guid> roomIds,
        CancellationToken cancellationToken)
    {
        if (Inner is not null)
        {
            return Inner.GetForRoomsAsync(propertyId, roomIds, cancellationToken);
        }

        var result = roomIds
            .Distinct()
            .ToDictionary(
                id => id,
                id => Snapshots.TryGetValue(id, out var snapshot)
                    ? snapshot
                    : RoomServiceabilitySnapshot.Available(id));
        return Task.FromResult<IReadOnlyDictionary<Guid, RoomServiceabilitySnapshot>>(result);
    }
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

    public InMemoryRoomServiceabilityLookup Serviceability { get; } = new();
    public RequestNeedsCleaningUseCase NeedsCleaning { get; }
    public EnsurePreparationRequiredUseCase EnsurePreparation { get; }
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

        NeedsCleaning = new RequestNeedsCleaningUseCase(Store, Employees, Serviceability, Workplace, Clock);
        EnsurePreparation = new EnsurePreparationRequiredUseCase(Store, Workplace, Clock);
        CompleteCleaning = new CompleteCleaningUseCase(Store, Employees, Serviceability, Workplace, Clock);
        Inspect = new InspectRoomUseCase(Store, Employees, Serviceability, Workplace, Clock);
        List = new ListRoomOperationsQuery(Store, Employees, Serviceability, Workplace);
        Detail = new GetRoomOperationsDetailQuery(Store, Employees, Serviceability, Workplace);

        SeedRoom(RoomId, "101");
    }

    public Room SeedReadyRoom(Guid id, string number, Guid? propertyId = null)
    {
        var room = SeedRoom(id, number, propertyId);
        MakeReady(room);
        return room;
    }

    public static void MakeReady(Room room)
    {
        if (room.CurrentReadiness == RoomReadiness.Dirty)
        {
            Assert.True(room.TryMarkClean(room.ReadinessCycleId, out _));
        }

        if (room.CurrentReadiness == RoomReadiness.Clean)
        {
            Assert.True(room.TryMarkInspected(out _));
        }

        if (room.CurrentReadiness == RoomReadiness.Inspected)
        {
            Assert.True(room.TryMarkReady(out _));
        }
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
