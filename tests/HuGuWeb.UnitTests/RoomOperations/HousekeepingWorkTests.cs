using HuGuWeb.RoomOperations.Application;
using HuGuWeb.RoomOperations.Domain;

namespace HuGuWeb.UnitTests.RoomOperations;

public class HousekeepingWorkTests
{
    [Fact]
    public async Task NeedsCleaning_CreatesOpenWork_AndKeepsDirty()
    {
        var harness = new RoomOperationsHarness();

        var result = await harness.NeedsCleaning.ExecuteAsync(harness.NeedsCleaningCommand(priority: TaskPriority.High), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(RoomReadiness.Dirty, result.Value!.Readiness);
        Assert.NotNull(result.Value.CurrentWork);
        Assert.Equal(HousekeepingWorkState.Open, result.Value.CurrentWork!.State);
        Assert.Equal(TaskPriority.High, result.Value.CurrentWork.Priority);
        Assert.Equal(harness.EmployeeId, result.Value.CurrentWork.AssignedEmployeeId);
        Assert.Equal(RoomReadiness.Dirty, harness.Store.Rooms[0].CurrentReadiness);
        Assert.Single(harness.Store.WorkItems);
    }

    [Fact]
    public async Task DuplicateActiveWork_IsRejected()
    {
        var harness = new RoomOperationsHarness();
        Assert.True((await harness.NeedsCleaning.ExecuteAsync(harness.NeedsCleaningCommand(), CancellationToken.None)).IsSuccess);

        var result = await harness.NeedsCleaning.ExecuteAsync(
            harness.NeedsCleaningCommand(employeeId: harness.OtherEmployeeId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("active-work-already-exists", result.Error!.Code);
        Assert.Single(harness.Store.WorkItems);
    }

    [Fact]
    public async Task Assignment_IsRequired()
    {
        var harness = new RoomOperationsHarness();

        var result = await harness.NeedsCleaning.ExecuteAsync(
            new RequestNeedsCleaningCommand(harness.RoomId, Guid.Empty, TaskPriority.Normal, harness.ActorUserId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("assignment-required", result.Error!.Code);
        Assert.Empty(harness.Store.WorkItems);
    }

    [Fact]
    public async Task NonexistentEmployee_IsRejected()
    {
        var harness = new RoomOperationsHarness();

        var result = await harness.NeedsCleaning.ExecuteAsync(
            harness.NeedsCleaningCommand(employeeId: Guid.CreateVersion7()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("employee-not-found", result.Error!.Code);
        Assert.Empty(harness.Store.WorkItems);
    }

    [Fact]
    public async Task CompleteCleaning_MovesDirtyToClean_AndCompletesWork()
    {
        var harness = new RoomOperationsHarness();
        var requested = await harness.NeedsCleaning.ExecuteAsync(harness.NeedsCleaningCommand(), CancellationToken.None);
        Assert.True(requested.IsSuccess);
        harness.Clock.Advance(TimeSpan.FromMinutes(20));

        var result = await harness.CompleteCleaning.ExecuteAsync(
            new CompleteCleaningCommand(requested.Value!.CurrentWork!.Id, harness.ActorUserId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(RoomReadiness.Clean, result.Value!.Readiness);
        Assert.Equal(HousekeepingWorkState.Completed, harness.Store.WorkItems[0].State);
        Assert.Equal(harness.EmployeeId, harness.Store.WorkItems[0].CompletedByEmployeeId);
        Assert.NotNull(harness.Store.WorkItems[0].CompletedAt);
        Assert.Contains(harness.Store.History, item => item.Readiness == RoomReadiness.Clean);
        Assert.DoesNotContain(harness.Store.History, item => item.Readiness == RoomReadiness.Ready);
    }

    [Fact]
    public async Task CompletingStaleWork_IsRejected()
    {
        var harness = new RoomOperationsHarness();
        var first = await harness.NeedsCleaning.ExecuteAsync(harness.NeedsCleaningCommand(), CancellationToken.None);
        Assert.True(first.IsSuccess);
        var staleId = first.Value!.CurrentWork!.Id;
        Assert.True((await harness.CompleteCleaning.ExecuteAsync(
            new CompleteCleaningCommand(staleId, harness.ActorUserId),
            CancellationToken.None)).IsSuccess);

        var accepted = await harness.Inspect.ExecuteAsync(
            new InspectRoomCommand(harness.RoomId, Accepted: true, Reason: null, harness.InspectorUserId),
            CancellationToken.None);
        Assert.True(accepted.IsSuccess);
        Assert.Equal(RoomReadiness.Ready, accepted.Value!.Readiness);

        var again = await harness.NeedsCleaning.ExecuteAsync(harness.NeedsCleaningCommand(), CancellationToken.None);
        Assert.True(again.IsSuccess);

        var stale = await harness.CompleteCleaning.ExecuteAsync(
            new CompleteCleaningCommand(staleId, harness.ActorUserId),
            CancellationToken.None);

        Assert.False(stale.IsSuccess);
        Assert.Equal("work-item-not-current", stale.Error!.Code);
        Assert.Equal(RoomReadiness.Dirty, harness.Store.Rooms[0].CurrentReadiness);
        Assert.Equal(HousekeepingWorkState.Completed, harness.Store.WorkItems[0].State);
    }

    [Fact]
    public async Task CompletedHistoricalWork_IsRetained()
    {
        var harness = new RoomOperationsHarness();
        var requested = await harness.NeedsCleaning.ExecuteAsync(harness.NeedsCleaningCommand(), CancellationToken.None);
        Assert.True((await harness.CompleteCleaning.ExecuteAsync(
            new CompleteCleaningCommand(requested.Value!.CurrentWork!.Id, harness.ActorUserId),
            CancellationToken.None)).IsSuccess);

        var rejected = await harness.Inspect.ExecuteAsync(
            new InspectRoomCommand(harness.RoomId, Accepted: false, Reason: "Lavabo eksik", harness.InspectorUserId),
            CancellationToken.None);

        Assert.True(rejected.IsSuccess);
        Assert.Equal(2, harness.Store.WorkItems.Count);
        Assert.Equal(HousekeepingWorkState.Completed, harness.Store.WorkItems[0].State);
        Assert.Equal(HousekeepingWorkState.Open, harness.Store.WorkItems[1].State);
        Assert.Equal(HousekeepingWorkOrigin.Rework, harness.Store.WorkItems[1].Origin);
        Assert.Equal(harness.EmployeeId, harness.Store.WorkItems[1].AssignedEmployeeId);
        Assert.NotEqual(harness.Store.WorkItems[0].Id, harness.Store.WorkItems[1].Id);
    }

    [Fact]
    public async Task Priority_DoesNotChangeReadiness()
    {
        var harness = new RoomOperationsHarness();
        var before = harness.Store.Rooms[0].CurrentReadiness;

        var result = await harness.NeedsCleaning.ExecuteAsync(
            harness.NeedsCleaningCommand(priority: TaskPriority.Urgent),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(before, result.Value!.Readiness);
        Assert.Equal(TaskPriority.Urgent, result.Value.CurrentWork!.Priority);
        Assert.Equal(RoomReadiness.Dirty, harness.Store.Rooms[0].CurrentReadiness);
    }

    [Fact]
    public void WorkAuthorization_DoesNotUsePositionNames()
    {
        var source = string.Concat(
            typeof(RequestNeedsCleaningUseCase).Assembly.GetTypes()
                .Select(type => type.FullName));

        Assert.DoesNotContain("KatGörevlisi", source, StringComparison.Ordinal);
        Assert.DoesNotContain("KatGorevlisi", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Supervisor", source, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderTaker", source, StringComparison.Ordinal);
    }
}
