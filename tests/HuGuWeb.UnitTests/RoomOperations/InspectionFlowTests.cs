using HuGuWeb.RoomOperations.Application;
using HuGuWeb.RoomOperations.Domain;

namespace HuGuWeb.UnitTests.RoomOperations;

public class InspectionFlowTests
{
    [Fact]
    public async Task Accept_RecordsInspectedThenReady()
    {
        var harness = new RoomOperationsHarness();
        await ReachCleanAsync(harness);

        var result = await harness.Inspect.ExecuteAsync(
            new InspectRoomCommand(harness.RoomId, Accepted: true, Reason: null, harness.InspectorUserId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(RoomReadiness.Ready, result.Value!.Readiness);
        Assert.Equal(RoomReadiness.Ready, harness.Store.Rooms[0].CurrentReadiness);
        Assert.Contains(harness.Store.History, item => item.Readiness == RoomReadiness.Inspected && item.Cause == ReadinessChangeCause.InspectionAccepted);
        Assert.Contains(harness.Store.History, item => item.Readiness == RoomReadiness.Ready && item.Cause == ReadinessChangeCause.InspectionAccepted);
        Assert.Single(harness.Store.Inspections);
        Assert.Equal(InspectionResult.Accepted, harness.Store.Inspections[0].Result);
        Assert.Null(harness.Store.Inspections[0].Reason);
    }

    [Fact]
    public async Task Reject_ReturnsDirty_RequiresReason_AndCreatesRework()
    {
        var harness = new RoomOperationsHarness();
        await ReachCleanAsync(harness);

        var result = await harness.Inspect.ExecuteAsync(
            new InspectRoomCommand(harness.RoomId, Accepted: false, Reason: "Banyo camı lekeli", harness.InspectorUserId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(RoomReadiness.Dirty, result.Value!.Readiness);
        Assert.Equal(RoomReadiness.Dirty, harness.Store.Rooms[0].CurrentReadiness);
        Assert.Equal(InspectionResult.Rejected, harness.Store.Inspections[0].Result);
        Assert.Equal("Banyo camı lekeli", harness.Store.Inspections[0].Reason);
        Assert.Equal(HousekeepingWorkOrigin.Rework, harness.Store.WorkItems[1].Origin);
        Assert.Equal(harness.EmployeeId, harness.Store.WorkItems[1].AssignedEmployeeId);
        Assert.Contains(harness.Store.History, item => item.Readiness == RoomReadiness.Dirty && item.Cause == ReadinessChangeCause.InspectionRejected);
        Assert.Contains(result.Value.InspectionHistory, item => item.Result == InspectionResult.Rejected);
    }

    [Fact]
    public async Task RejectWithoutReason_IsRejected_AndHistoryUnchanged()
    {
        var harness = new RoomOperationsHarness();
        await ReachCleanAsync(harness);
        var historyCount = harness.Store.History.Count;

        var result = await harness.Inspect.ExecuteAsync(
            new InspectRoomCommand(harness.RoomId, Accepted: false, Reason: null, harness.InspectorUserId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("rejection-reason-required", result.Error!.Code);
        Assert.Equal(RoomReadiness.Clean, harness.Store.Rooms[0].CurrentReadiness);
        Assert.Empty(harness.Store.Inspections);
        Assert.Equal(historyCount, harness.Store.History.Count);
    }

    [Fact]
    public async Task Reject_DoesNotEraseHistory_AndLaterAcceptKeepsRejection()
    {
        var harness = new RoomOperationsHarness();
        await ReachCleanAsync(harness);
        Assert.True((await harness.Inspect.ExecuteAsync(
            new InspectRoomCommand(harness.RoomId, Accepted: false, Reason: "Toz kaldı", harness.InspectorUserId),
            CancellationToken.None)).IsSuccess);

        var rework = harness.Store.WorkItems.Single(item => item.IsOpen);
        Assert.True((await harness.CompleteCleaning.ExecuteAsync(
            new CompleteCleaningCommand(rework.Id, harness.ActorUserId),
            CancellationToken.None)).IsSuccess);

        var accepted = await harness.Inspect.ExecuteAsync(
            new InspectRoomCommand(harness.RoomId, Accepted: true, Reason: null, harness.InspectorUserId),
            CancellationToken.None);

        Assert.True(accepted.IsSuccess);
        Assert.Equal(RoomReadiness.Ready, accepted.Value!.Readiness);
        Assert.Equal(2, harness.Store.Inspections.Count);
        Assert.Contains(harness.Store.Inspections, item => item.Result == InspectionResult.Rejected);
        Assert.Contains(harness.Store.Inspections, item => item.Result == InspectionResult.Accepted);
        Assert.Contains(accepted.Value.InspectionHistory, item => item.Result == InspectionResult.Rejected);
        Assert.Contains(accepted.Value.ReadinessHistory, item => item.Readiness == RoomReadiness.Inspected);
    }

    [Fact]
    public async Task Inspection_IsNotAllowed_WhenDirty()
    {
        var harness = new RoomOperationsHarness();

        var result = await harness.Inspect.ExecuteAsync(
            new InspectRoomCommand(harness.RoomId, Accepted: true, Reason: null, harness.InspectorUserId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("inspection-not-allowed", result.Error!.Code);
    }

    [Fact]
    public async Task StaleWork_AfterNewerCycle_CannotMarkClean()
    {
        var harness = new RoomOperationsHarness();
        var first = await harness.NeedsCleaning.ExecuteAsync(harness.NeedsCleaningCommand(), CancellationToken.None);
        var firstWork = first.Value!.CurrentWork!;
        var firstCycle = harness.Store.Rooms[0].ReadinessCycleId;

        Assert.True(harness.Store.Rooms[0].TryMarkDirtyForNewCycle(Guid.CreateVersion7(), out _));
        Assert.NotEqual(firstCycle, harness.Store.Rooms[0].ReadinessCycleId);

        var stale = await harness.CompleteCleaning.ExecuteAsync(
            new CompleteCleaningCommand(firstWork.Id, harness.ActorUserId),
            CancellationToken.None);

        Assert.False(stale.IsSuccess);
        Assert.Equal("stale-work-item", stale.Error!.Code);
        Assert.Equal(RoomReadiness.Dirty, harness.Store.Rooms[0].CurrentReadiness);
        Assert.True(harness.Store.WorkItems[0].IsOpen);
    }

    private static async Task ReachCleanAsync(RoomOperationsHarness harness)
    {
        var requested = await harness.NeedsCleaning.ExecuteAsync(harness.NeedsCleaningCommand(), CancellationToken.None);
        Assert.True(requested.IsSuccess);
        var completed = await harness.CompleteCleaning.ExecuteAsync(
            new CompleteCleaningCommand(requested.Value!.CurrentWork!.Id, harness.ActorUserId),
            CancellationToken.None);
        Assert.True(completed.IsSuccess);
        Assert.Equal(RoomReadiness.Clean, completed.Value!.Readiness);
    }
}
