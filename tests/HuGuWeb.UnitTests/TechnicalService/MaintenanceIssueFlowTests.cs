using HuGuWeb.TechnicalService.Application;
using HuGuWeb.TechnicalService.Domain;

namespace HuGuWeb.UnitTests.TechnicalService;

public class MaintenanceIssueFlowTests
{
    [Fact]
    public async Task Create_RecordsOpenIssue_AndHistory()
    {
        var harness = new TechnicalServiceHarness();

        var result = await harness.Create.ExecuteAsync(harness.CreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(MaintenanceIssueStatus.Open, result.Value!.Status);
        Assert.Equal("101", result.Value.RoomNumber);
        Assert.Equal(MaintenancePriority.High, result.Value.Priority);
        Assert.Equal("assign", result.Value.NeededAction);
        Assert.Contains(result.Value.History, item => item.EventType == MaintenanceIssueHistoryEvent.Created);
        Assert.Single(harness.Store.Issues);
    }

    [Fact]
    public async Task Create_WithAssignee_RecordsAssignmentHistory()
    {
        var harness = new TechnicalServiceHarness();

        var result = await harness.Create.ExecuteAsync(
            harness.CreateCommand(assignedEmployeeId: harness.EmployeeId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(harness.EmployeeId, result.Value!.AssignedEmployeeId);
        Assert.Equal("start", result.Value.NeededAction);
        Assert.Contains(result.Value.History, item => item.EventType == MaintenanceIssueHistoryEvent.Assigned);
    }

    [Fact]
    public async Task InvalidRoom_IsRejected()
    {
        var harness = new TechnicalServiceHarness();

        var result = await harness.Create.ExecuteAsync(
            harness.CreateCommand(roomId: Guid.CreateVersion7()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("room-not-found", result.Error!.Code);
        Assert.Empty(harness.Store.Issues);
    }

    [Fact]
    public async Task InactiveRoom_IsRejected()
    {
        var harness = new TechnicalServiceHarness();

        var result = await harness.Create.ExecuteAsync(
            harness.CreateCommand(roomId: harness.InactiveRoomId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("room-inactive", result.Error!.Code);
    }

    [Fact]
    public async Task InvalidEmployee_IsRejected()
    {
        var harness = new TechnicalServiceHarness();

        var result = await harness.Create.ExecuteAsync(
            harness.CreateCommand(assignedEmployeeId: Guid.CreateVersion7()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("employee-not-found", result.Error!.Code);
        Assert.Empty(harness.Store.Issues);
    }

    [Fact]
    public async Task InvalidPriority_IsRejected()
    {
        var harness = new TechnicalServiceHarness();

        var result = await harness.Create.ExecuteAsync(
            harness.CreateCommand() with { Priority = (MaintenancePriority)99 },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid-priority", result.Error!.Code);
    }

    [Fact]
    public async Task BlockingWithoutOutage_IsRejected()
    {
        var harness = new TechnicalServiceHarness();

        var result = await harness.Create.ExecuteAsync(
            harness.CreateCommand(blocksRoomUse: true),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid-blocking", result.Error!.Code);
    }

    [Fact]
    public async Task Open_ToInProgress_RequiresAssignee()
    {
        var harness = new TechnicalServiceHarness();
        var created = await harness.Create.ExecuteAsync(harness.CreateCommand(), CancellationToken.None);
        Assert.True(created.IsSuccess);

        var started = await harness.Start.ExecuteAsync(
            new StartWorkCommand(created.Value!.Id, created.Value.Version, harness.ActorUserId),
            CancellationToken.None);

        Assert.False(started.IsSuccess);
        Assert.Equal("assignment-required", started.Error!.Code);
        Assert.Equal(MaintenanceIssueStatus.Open, harness.Store.Issues[0].Status);
    }

    [Fact]
    public async Task Open_ToInProgress_WhenAssigned()
    {
        var harness = new TechnicalServiceHarness();
        var created = await OpenAssignedAsync(harness);

        var started = await harness.Start.ExecuteAsync(
            new StartWorkCommand(created.Id, created.Version, harness.ActorUserId),
            CancellationToken.None);

        Assert.True(started.IsSuccess);
        Assert.Equal(MaintenanceIssueStatus.InProgress, started.Value!.Status);
        Assert.Equal("resolve", started.Value.NeededAction);
        Assert.NotNull(started.Value.StartedAt);
        Assert.Contains(started.Value.History, item => item.EventType == MaintenanceIssueHistoryEvent.Started);
    }

    [Fact]
    public async Task InProgress_ToResolved()
    {
        var harness = new TechnicalServiceHarness();
        var inProgress = await StartAsync(harness);

        var resolved = await harness.Resolve.ExecuteAsync(
            new ResolveWorkCommand(inProgress.Id, "Klima değişti.", PreparationImpact.None, inProgress.Version, harness.ActorUserId),
            CancellationToken.None);

        Assert.True(resolved.IsSuccess);
        Assert.Equal(MaintenanceIssueStatus.Resolved, resolved.Value!.Status);
        Assert.Equal(PreparationImpact.None, resolved.Value.PreparationImpact);
        Assert.Empty(harness.Preparation.RequestedRooms);
        Assert.Contains(resolved.Value.History, item => item.EventType == MaintenanceIssueHistoryEvent.Resolved);
    }

    [Fact]
    public async Task InProgress_ToUnableToResolve_RequiresNote()
    {
        var harness = new TechnicalServiceHarness();
        var inProgress = await StartAsync(harness);

        var missing = await harness.Unable.ExecuteAsync(
            new MarkUnableToResolveCommand(inProgress.Id, "  ", inProgress.Version, harness.ActorUserId),
            CancellationToken.None);

        Assert.False(missing.IsSuccess);
        Assert.Equal("note-required", missing.Error!.Code);

        var unable = await harness.Unable.ExecuteAsync(
            new MarkUnableToResolveCommand(inProgress.Id, "Parça yok.", inProgress.Version, harness.ActorUserId),
            CancellationToken.None);

        Assert.True(unable.IsSuccess);
        Assert.Equal(MaintenanceIssueStatus.UnableToResolve, unable.Value!.Status);
        Assert.Equal("Parça yok.", unable.Value.UnableToResolveNote);
        Assert.Equal("resume", unable.Value.NeededAction);
    }

    [Fact]
    public async Task UnableToResolve_ToInProgress_ThenResolved()
    {
        var harness = new TechnicalServiceHarness();
        var inProgress = await StartAsync(harness);
        var unable = await harness.Unable.ExecuteAsync(
            new MarkUnableToResolveCommand(inProgress.Id, "Parça yok.", inProgress.Version, harness.ActorUserId),
            CancellationToken.None);
        Assert.True(unable.IsSuccess);

        var resumed = await harness.Resume.ExecuteAsync(
            new ResumeWorkCommand(unable.Value!.Id, unable.Value.Version, harness.ActorUserId),
            CancellationToken.None);

        Assert.True(resumed.IsSuccess);
        Assert.Equal(MaintenanceIssueStatus.InProgress, resumed.Value!.Status);
        Assert.Equal("Parça yok.", resumed.Value.UnableToResolveNote);
        Assert.Contains(resumed.Value.History, item => item.EventType == MaintenanceIssueHistoryEvent.UnableToResolve);
        Assert.Contains(resumed.Value.History, item => item.EventType == MaintenanceIssueHistoryEvent.Resumed);

        var resolved = await harness.Resolve.ExecuteAsync(
            new ResolveWorkCommand(
                resumed.Value.Id,
                "Parça geldi, klima çalışıyor.",
                PreparationImpact.None,
                resumed.Value.Version,
                harness.ActorUserId),
            CancellationToken.None);

        Assert.True(resolved.IsSuccess);
        Assert.Equal(MaintenanceIssueStatus.Resolved, resolved.Value!.Status);
        Assert.Equal(6, resolved.Value.History.Count);
        Assert.Contains(resolved.Value.History, item => item.EventType == MaintenanceIssueHistoryEvent.Created);
        Assert.Contains(resolved.Value.History, item => item.EventType == MaintenanceIssueHistoryEvent.Assigned);
        Assert.Contains(resolved.Value.History, item => item.EventType == MaintenanceIssueHistoryEvent.Started);
        Assert.Contains(resolved.Value.History, item => item.EventType == MaintenanceIssueHistoryEvent.UnableToResolve);
        Assert.Contains(resolved.Value.History, item => item.EventType == MaintenanceIssueHistoryEvent.Resumed);
        Assert.Contains(resolved.Value.History, item => item.EventType == MaintenanceIssueHistoryEvent.Resolved);
    }

    [Fact]
    public async Task InvalidTransitions_AreRejected()
    {
        var harness = new TechnicalServiceHarness();
        var created = await harness.Create.ExecuteAsync(harness.CreateCommand(), CancellationToken.None);
        Assert.True(created.IsSuccess);

        var resolveFromOpen = await harness.Resolve.ExecuteAsync(
            new ResolveWorkCommand(created.Value!.Id, "Done", PreparationImpact.None, created.Value.Version, harness.ActorUserId),
            CancellationToken.None);
        Assert.Equal("invalid-transition", resolveFromOpen.Error!.Code);

        var resumeFromOpen = await harness.Resume.ExecuteAsync(
            new ResumeWorkCommand(created.Value.Id, created.Value.Version, harness.ActorUserId),
            CancellationToken.None);
        Assert.Equal("invalid-transition", resumeFromOpen.Error!.Code);

        var assigned = await OpenAssignedAsync(harness);
        var started = await harness.Start.ExecuteAsync(
            new StartWorkCommand(assigned.Id, assigned.Version, harness.ActorUserId),
            CancellationToken.None);
        Assert.True(started.IsSuccess);

        var startAgain = await harness.Start.ExecuteAsync(
            new StartWorkCommand(started.Value!.Id, started.Value.Version, harness.ActorUserId),
            CancellationToken.None);
        Assert.Equal("invalid-transition", startAgain.Error!.Code);
    }

    [Fact]
    public async Task Reassignment_IsAuditable()
    {
        var harness = new TechnicalServiceHarness();
        var created = await OpenAssignedAsync(harness);

        var reassigned = await harness.Assign.ExecuteAsync(
            new AssignIssueCommand(created.Id, harness.OtherEmployeeId, created.Version, harness.ActorUserId),
            CancellationToken.None);

        Assert.True(reassigned.IsSuccess);
        Assert.Equal(harness.OtherEmployeeId, reassigned.Value!.AssignedEmployeeId);
        Assert.Contains(reassigned.Value.History, item => item.EventType == MaintenanceIssueHistoryEvent.Reassigned);
    }

    [Fact]
    public async Task History_IsPreserved_WhenWorkResumes()
    {
        var harness = new TechnicalServiceHarness();
        var inProgress = await StartAsync(harness);
        var unable = await harness.Unable.ExecuteAsync(
            new MarkUnableToResolveCommand(inProgress.Id, "Bekleniyor.", inProgress.Version, harness.ActorUserId),
            CancellationToken.None);
        Assert.True(unable.IsSuccess);
        var historyCount = unable.Value!.History.Count;

        var resumed = await harness.Resume.ExecuteAsync(
            new ResumeWorkCommand(unable.Value.Id, unable.Value.Version, harness.ActorUserId),
            CancellationToken.None);

        Assert.True(resumed.IsSuccess);
        Assert.True(resumed.Value!.History.Count > historyCount);
        Assert.Equal("Bekleniyor.", resumed.Value.UnableToResolveNote);
        Assert.Contains(resumed.Value.History, item => item.Note == "Bekleniyor.");
    }

    [Fact]
    public async Task RequiresPreparation_CallsRoomOperationsConsume()
    {
        var harness = new TechnicalServiceHarness();
        var inProgress = await StartAsync(harness);

        var resolved = await harness.Resolve.ExecuteAsync(
            new ResolveWorkCommand(
                inProgress.Id,
                "Tamir bitti, oda kirlendi.",
                PreparationImpact.RequiresPreparation,
                inProgress.Version,
                harness.ActorUserId),
            CancellationToken.None);

        Assert.True(resolved.IsSuccess);
        Assert.Equal(new[] { harness.RoomId }, harness.Preparation.RequestedRooms);
        Assert.Equal(PreparationImpact.RequiresPreparation, resolved.Value!.PreparationImpact);
    }

    [Fact]
    public async Task StaleVersion_IsRejected()
    {
        var harness = new TechnicalServiceHarness();
        var created = await OpenAssignedAsync(harness);

        var stale = await harness.Start.ExecuteAsync(
            new StartWorkCommand(created.Id, created.Version - 1, harness.ActorUserId),
            CancellationToken.None);

        Assert.False(stale.IsSuccess);
        Assert.Equal("stale-issue", stale.Error!.Code);
        Assert.Equal(MaintenanceIssueStatus.Open, harness.Store.Issues[0].Status);
    }

    [Fact]
    public async Task ConcurrentSave_IsRejected()
    {
        var harness = new TechnicalServiceHarness();
        var created = await OpenAssignedAsync(harness);
        harness.Store.ThrowConcurrencyOnSave = true;

        var result = await harness.Start.ExecuteAsync(
            new StartWorkCommand(created.Id, created.Version, harness.ActorUserId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("stale-issue", result.Error!.Code);
    }

    [Fact]
    public async Task BlockingIssue_DerivesOutOfServiceOverOutOfOrder()
    {
        var harness = new TechnicalServiceHarness();
        var first = await harness.Create.ExecuteAsync(
            harness.CreateCommand(blocksRoomUse: true, outage: OutageClassification.OutOfOrder),
            CancellationToken.None);
        Assert.True(first.IsSuccess);
        Assert.Equal(RoomServiceabilityState.OutOfOrder, first.Value!.RoomServiceability);

        var second = await harness.Create.ExecuteAsync(
            harness.CreateCommand(
                description: "Tesisat patladı",
                blocksRoomUse: true,
                outage: OutageClassification.OutOfService),
            CancellationToken.None);

        Assert.True(second.IsSuccess);
        Assert.Equal(RoomServiceabilityState.OutOfService, second.Value!.RoomServiceability);
        var detail = await harness.Detail.ExecuteAsync(first.Value.Id, CancellationToken.None);
        Assert.Equal(RoomServiceabilityState.OutOfService, detail.Value!.RoomServiceability);
    }

    [Fact]
    public async Task GoverningIssue_PrefersOutOfService_ThenOldestOutOfOrder()
    {
        var harness = new TechnicalServiceHarness();
        var first = await harness.Create.ExecuteAsync(
            harness.CreateCommand(description: "Klima", blocksRoomUse: true, outage: OutageClassification.OutOfOrder),
            CancellationToken.None);
        var second = await harness.Create.ExecuteAsync(
            harness.CreateCommand(description: "Tesisat", blocksRoomUse: true, outage: OutageClassification.OutOfService),
            CancellationToken.None);
        await harness.Create.ExecuteAsync(
            harness.CreateCommand(description: "TV", blocksRoomUse: true, outage: OutageClassification.OutOfOrder),
            CancellationToken.None);

        var view = RoomServiceabilityProjection.Project(harness.RoomId, harness.Store.Issues);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(RoomServiceabilityState.OutOfService, view.Serviceability);
        Assert.Equal(second.Value!.Id, view.GoverningIssueId);
        Assert.Equal("Tesisat", view.GoverningIssueDescription);
    }

    [Fact]
    public async Task NonBlockingIssue_LeavesRoomServiceable()
    {
        var harness = new TechnicalServiceHarness();
        var result = await harness.Create.ExecuteAsync(harness.CreateCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(RoomServiceabilityState.Serviceable, result.Value!.RoomServiceability);
        Assert.False(result.Value.BlocksRoomUse);
    }

    [Fact]
    public void Resolved_IsTerminal()
    {
        Assert.True(MaintenanceIssue.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Arıza",
            MaintenancePriority.Normal,
            Guid.CreateVersion7(),
            null,
            null,
            false,
            null,
            DateTimeOffset.UtcNow,
            out var issue,
            out _));
        Assert.True(issue!.TryStart(DateTimeOffset.UtcNow, out _));
        Assert.True(issue.TryResolve("Tamam", PreparationImpact.None, DateTimeOffset.UtcNow, out _));
        Assert.False(issue.TryAssign(Guid.CreateVersion7(), out _));
        Assert.False(issue.TryStart(DateTimeOffset.UtcNow, out _));
        Assert.False(issue.TryResume(out _));
    }

    private static async Task<MaintenanceIssueDetail> OpenAssignedAsync(TechnicalServiceHarness harness)
    {
        var created = await harness.Create.ExecuteAsync(
            harness.CreateCommand(assignedEmployeeId: harness.EmployeeId),
            CancellationToken.None);
        Assert.True(created.IsSuccess);
        return created.Value!;
    }

    private static async Task<MaintenanceIssueDetail> StartAsync(TechnicalServiceHarness harness)
    {
        var created = await OpenAssignedAsync(harness);
        var started = await harness.Start.ExecuteAsync(
            new StartWorkCommand(created.Id, created.Version, harness.ActorUserId),
            CancellationToken.None);
        Assert.True(started.IsSuccess);
        return started.Value!;
    }
}
