using HuGuWeb.TechnicalService.Application;
using HuGuWeb.TechnicalService.Domain;

namespace HuGuWeb.UnitTests.TechnicalService;

internal sealed class FakeTechnicalServiceClock : ITechnicalServiceClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 8, 23, 11, 0, 0, TimeSpan.Zero);

    public void Advance(TimeSpan duration) => UtcNow = UtcNow.Add(duration);
}

internal sealed class FixedTechnicalServiceWorkplace(Guid propertyId) : ITechnicalServiceWorkplace
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

internal sealed class InMemoryRoomDirectory : IRoomIdentityDirectory
{
    public List<KnownRoom> Rooms { get; } = [];

    public Task<KnownRoom?> FindAsync(Guid roomId, CancellationToken cancellationToken) =>
        Task.FromResult(Rooms.FirstOrDefault(item => item.RoomId == roomId));

    public Task<IReadOnlyList<KnownRoom>> ListActiveAsync(Guid propertyId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<KnownRoom>>(
            Rooms.Where(item => item.PropertyId == propertyId && item.IsActive).ToArray());

    public Task<IReadOnlyDictionary<Guid, KnownRoom>> GetAsync(
        IReadOnlyCollection<Guid> roomIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, KnownRoom>>(
            Rooms.Where(item => roomIds.Contains(item.RoomId)).ToDictionary(item => item.RoomId));
}

internal sealed class InMemoryPreparationConsumer : IRoomPreparationImpactConsumer
{
    public List<Guid> RequestedRooms { get; } = [];
    public TechnicalServiceError? FailWith { get; set; }

    public Task<TechnicalServiceResult<bool>> EnsurePreparationRequiredAsync(
        Guid roomId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        if (FailWith is not null)
        {
            return Task.FromResult<TechnicalServiceResult<bool>>(FailWith);
        }

        RequestedRooms.Add(roomId);
        return Task.FromResult<TechnicalServiceResult<bool>>(true);
    }
}

internal sealed class InMemoryTechnicalServiceStore : ITechnicalServiceStore
{
    public List<MaintenanceIssue> Issues { get; } = [];
    public List<MaintenanceIssueCategory> Categories { get; } = [];
    public List<MaintenanceIssueHistoryEntry> History { get; } = [];
    public bool ThrowConcurrencyOnSave { get; set; }

    public Task<MaintenanceIssue?> GetIssueAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Issues.FirstOrDefault(item => item.Id == id));

    public Task<IReadOnlyList<MaintenanceIssue>> ListIssuesAsync(Guid propertyId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MaintenanceIssue>>(Issues.Where(item => item.PropertyId == propertyId).ToArray());

    public Task<IReadOnlyList<MaintenanceIssue>> ListIssuesForRoomAsync(Guid roomId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MaintenanceIssue>>(Issues.Where(item => item.RoomId == roomId).ToArray());

    public Task<MaintenanceIssueCategory?> GetCategoryAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Categories.FirstOrDefault(item => item.Id == id));

    public Task<IReadOnlyList<MaintenanceIssueCategory>> ListCategoriesAsync(
        Guid propertyId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MaintenanceIssueCategory>>(
            Categories.Where(item => item.PropertyId == propertyId).ToArray());

    public Task<IReadOnlyList<MaintenanceIssueHistoryEntry>> ListHistoryAsync(
        Guid issueId,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<MaintenanceIssueHistoryEntry>>(
            History.Where(item => item.IssueId == issueId).ToArray());

    public void AddIssue(MaintenanceIssue issue) => Issues.Add(issue);

    public void AddCategory(MaintenanceIssueCategory category) => Categories.Add(category);

    public void AddHistory(MaintenanceIssueHistoryEntry entry) => History.Add(entry);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (ThrowConcurrencyOnSave)
        {
            throw new IssueConcurrencyConflictException();
        }

        return Task.CompletedTask;
    }
}

internal sealed class TechnicalServiceHarness
{
    public Guid PropertyId { get; }
    public Guid OtherPropertyId { get; } = Guid.CreateVersion7();
    public Guid RoomId { get; } = Guid.CreateVersion7();
    public Guid InactiveRoomId { get; } = Guid.CreateVersion7();
    public Guid CategoryId { get; } = Guid.CreateVersion7();
    public Guid EmployeeId { get; } = Guid.CreateVersion7();
    public Guid OtherEmployeeId { get; } = Guid.CreateVersion7();
    public Guid ActorUserId { get; } = Guid.CreateVersion7();

    public FakeTechnicalServiceClock Clock { get; } = new();
    public InMemoryTechnicalServiceStore Store { get; } = new();
    public InMemoryAssignableEmployees Employees { get; } = new();
    public InMemoryRoomDirectory Rooms { get; } = new();
    public InMemoryPreparationConsumer Preparation { get; } = new();
    public FixedTechnicalServiceWorkplace Workplace { get; }

    public CreateIssueUseCase Create { get; }
    public AssignIssueUseCase Assign { get; }
    public ChangePriorityUseCase ChangePriority { get; }
    public ChangeBlockingUseCase ChangeBlocking { get; }
    public StartWorkUseCase Start { get; }
    public MarkUnableToResolveUseCase Unable { get; }
    public ResumeWorkUseCase Resume { get; }
    public ResolveWorkUseCase Resolve { get; }
    public ListIssuesQuery List { get; }
    public GetIssueDetailQuery Detail { get; }

    public TechnicalServiceHarness(Guid? propertyId = null)
    {
        PropertyId = propertyId ?? Guid.CreateVersion7();
        Workplace = new FixedTechnicalServiceWorkplace(PropertyId);
        Employees.Assignable.Add(new AssignableEmployee(EmployeeId, "Can", "Yılmaz", "DEV-2001"));
        Employees.Assignable.Add(new AssignableEmployee(OtherEmployeeId, "Elif", "Demir", "DEV-2002"));
        Employees.Known.AddRange(Employees.Assignable);
        Rooms.Rooms.Add(new KnownRoom(RoomId, PropertyId, "101", IsActive: true));
        Rooms.Rooms.Add(new KnownRoom(InactiveRoomId, PropertyId, "999", IsActive: false));
        Assert.True(MaintenanceIssueCategory.TryCreate(CategoryId, PropertyId, "Klima", out var category, out _));
        Store.Categories.Add(category!);

        Create = new CreateIssueUseCase(Store, Employees, Rooms, Workplace, Clock);
        Assign = new AssignIssueUseCase(Store, Employees, Rooms, Workplace, Clock);
        ChangePriority = new ChangePriorityUseCase(Store, Employees, Rooms, Workplace, Clock);
        ChangeBlocking = new ChangeBlockingUseCase(Store, Employees, Rooms, Workplace, Clock);
        Start = new StartWorkUseCase(Store, Employees, Rooms, Workplace, Clock);
        Unable = new MarkUnableToResolveUseCase(Store, Employees, Rooms, Workplace, Clock);
        Resume = new ResumeWorkUseCase(Store, Employees, Rooms, Workplace, Clock);
        Resolve = new ResolveWorkUseCase(Store, Employees, Rooms, Preparation, Workplace, Clock);
        List = new ListIssuesQuery(Store, Employees, Rooms, Workplace);
        Detail = new GetIssueDetailQuery(Store, Employees, Rooms, Workplace);
    }

    public void AddRoom(Guid roomId, string number, bool isActive = true) =>
        Rooms.Rooms.Add(new KnownRoom(roomId, PropertyId, number, isActive));

    public CreateIssueCommand CreateCommand(
        Guid? roomId = null,
        Guid? categoryId = null,
        string description = "Klima soğutmuyor",
        MaintenancePriority priority = MaintenancePriority.High,
        Guid? assignedEmployeeId = null,
        bool blocksRoomUse = false,
        OutageClassification? outage = null) =>
        new(
            roomId ?? RoomId,
            categoryId ?? CategoryId,
            description,
            priority,
            assignedEmployeeId,
            ReportedByEmployeeId: null,
            OriginNote: null,
            blocksRoomUse,
            outage,
            ActorUserId);
}
