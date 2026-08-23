using HuGuWeb.Api.Authorization;
using HuGuWeb.Api.Identity;
using HuGuWeb.RoomOperations.Application;
using HuGuWeb.RoomOperations.Domain;
using HuGuWeb.TechnicalService.Application;
using HuGuWeb.TechnicalService.Domain;
using HuGuWeb.TechnicalService.Infrastructure;
using HuGuWeb.UnitTests.RoomOperations;
using HuGuWeb.UnitTests.TechnicalService;

namespace HuGuWeb.UnitTests.CrossDomain;

public class RoomOperationalConditionTests
{
    [Fact]
    public async Task Ready_WithoutTechnicalRestriction_IsServiceable()
    {
        var fixture = await OperationalConditionFixture.ReadyRoomAsync();

        var listed = await fixture.Rooms.List.ExecuteAsync(CancellationToken.None);
        var detail = await fixture.Rooms.Detail.ExecuteAsync(fixture.RoomId, CancellationToken.None);

        Assert.True(listed.IsSuccess);
        var room = Assert.Single(listed.Value!, item => item.Id == fixture.RoomId);
        Assert.Equal(RoomReadiness.Ready, room.Readiness);
        Assert.Equal("Serviceable", room.TechnicalServiceability);
        Assert.False(room.HasActiveTechnicalIssue);
        Assert.Equal(RoomReadiness.Ready, detail.Value!.Readiness);
        Assert.Equal("Serviceable", detail.Value.TechnicalServiceability);
        Assert.False(detail.Value.HasActiveTechnicalIssue);
        Assert.Null(detail.Value.GoverningIssueId);
        Assert.Null(detail.Value.ActiveTechnicalIssueDescription);
    }

    [Fact]
    public async Task Ready_WithBlockingSameDayIssue_StaysReadyAndReportsOutOfOrder()
    {
        var fixture = await OperationalConditionFixture.ReadyRoomAsync();
        var created = await fixture.CreateBlockingAsync("Klima soğutmuyor", OutageClassification.OutOfOrder);

        var listed = await fixture.Rooms.List.ExecuteAsync(CancellationToken.None);
        var detail = await fixture.Rooms.Detail.ExecuteAsync(fixture.RoomId, CancellationToken.None);

        var room = Assert.Single(listed.Value!, item => item.Id == fixture.RoomId);
        Assert.Equal(RoomReadiness.Ready, room.Readiness);
        Assert.Equal("OutOfOrder", room.TechnicalServiceability);
        Assert.True(room.HasActiveTechnicalIssue);
        Assert.Equal(RoomReadiness.Ready, fixture.Rooms.Store.Rooms.Single(item => item.Id == fixture.RoomId).CurrentReadiness);
        Assert.Equal(RoomReadiness.Ready, detail.Value!.Readiness);
        Assert.Equal("OutOfOrder", detail.Value.TechnicalServiceability);
        Assert.True(detail.Value.HasActiveTechnicalIssue);
        Assert.Equal(created.Id, detail.Value.GoverningIssueId);
        Assert.Equal("Klima soğutmuyor", detail.Value.ActiveTechnicalIssueDescription);
    }

    [Fact]
    public async Task Dirty_AndTechnicalUnusability_Coexist()
    {
        var fixture = await OperationalConditionFixture.ReadyRoomAsync();
        await fixture.CreateBlockingAsync("Klima soğutmuyor", OutageClassification.OutOfOrder);
        Assert.True(fixture.Room.TryMarkDirtyForNewCycle(Guid.CreateVersion7(), out _));

        var listed = await fixture.Rooms.List.ExecuteAsync(CancellationToken.None);

        var room = Assert.Single(listed.Value!, item => item.Id == fixture.RoomId);
        Assert.Equal(RoomReadiness.Dirty, room.Readiness);
        Assert.Equal("OutOfOrder", room.TechnicalServiceability);
        Assert.True(room.HasActiveTechnicalIssue);
    }

    [Fact]
    public async Task StartTechnicalWork_DoesNotChangeRoomReadiness()
    {
        var fixture = await OperationalConditionFixture.ReadyRoomAsync();
        var created = await fixture.CreateBlockingAsync("Klima soğutmuyor", OutageClassification.OutOfOrder);

        var started = await fixture.Maintenance.Start.ExecuteAsync(
            new StartWorkCommand(created.Id, created.Version, fixture.Maintenance.ActorUserId),
            CancellationToken.None);

        Assert.True(started.IsSuccess);
        Assert.Equal(MaintenanceIssueStatus.InProgress, started.Value!.Status);
        Assert.Equal(RoomReadiness.Ready, fixture.Room.CurrentReadiness);
        var detail = await fixture.Rooms.Detail.ExecuteAsync(fixture.RoomId, CancellationToken.None);
        Assert.Equal(RoomReadiness.Ready, detail.Value!.Readiness);
        Assert.Equal("OutOfOrder", detail.Value.TechnicalServiceability);
    }

    [Fact]
    public async Task UnableToResolve_KeepsReadinessAndUnusability()
    {
        var fixture = await OperationalConditionFixture.ReadyRoomAsync();
        var created = await fixture.CreateBlockingAsync("Klima soğutmuyor", OutageClassification.OutOfOrder);
        var started = await fixture.Maintenance.Start.ExecuteAsync(
            new StartWorkCommand(created.Id, created.Version, fixture.Maintenance.ActorUserId),
            CancellationToken.None);

        var unable = await fixture.Maintenance.Unable.ExecuteAsync(
            new MarkUnableToResolveCommand(
                started.Value!.Id,
                "Parça yok.",
                started.Value.Version,
                fixture.Maintenance.ActorUserId),
            CancellationToken.None);

        Assert.True(unable.IsSuccess);
        Assert.Equal(MaintenanceIssueStatus.UnableToResolve, unable.Value!.Status);
        Assert.Equal(RoomReadiness.Ready, fixture.Room.CurrentReadiness);
        var detail = await fixture.Rooms.Detail.ExecuteAsync(fixture.RoomId, CancellationToken.None);
        Assert.Equal(RoomReadiness.Ready, detail.Value!.Readiness);
        Assert.Equal("OutOfOrder", detail.Value.TechnicalServiceability);
        Assert.True(detail.Value.HasActiveTechnicalIssue);
    }

    [Fact]
    public async Task ResolveFinalBlockingIssue_ClearsRestriction_WithoutChangingReadiness()
    {
        var fixture = await OperationalConditionFixture.ReadyRoomAsync();
        var created = await fixture.CreateBlockingAsync("Klima soğutmuyor", OutageClassification.OutOfOrder);
        var started = await fixture.Maintenance.Start.ExecuteAsync(
            new StartWorkCommand(created.Id, created.Version, fixture.Maintenance.ActorUserId),
            CancellationToken.None);

        var resolved = await fixture.Maintenance.Resolve.ExecuteAsync(
            new ResolveWorkCommand(
                started.Value!.Id,
                "Klima değişti.",
                PreparationImpact.None,
                started.Value.Version,
                fixture.Maintenance.ActorUserId),
            CancellationToken.None);

        Assert.True(resolved.IsSuccess);
        Assert.Empty(fixture.Maintenance.Preparation.RequestedRooms);
        Assert.Equal(RoomReadiness.Ready, fixture.Room.CurrentReadiness);
        var detail = await fixture.Rooms.Detail.ExecuteAsync(fixture.RoomId, CancellationToken.None);
        Assert.Equal(RoomReadiness.Ready, detail.Value!.Readiness);
        Assert.Equal("Serviceable", detail.Value.TechnicalServiceability);
        Assert.False(detail.Value.HasActiveTechnicalIssue);
        Assert.Null(detail.Value.GoverningIssueId);
        Assert.Null(detail.Value.ActiveTechnicalIssueDescription);
    }

    [Fact]
    public async Task ResolvingOneOfTwoBlockingIssues_KeepsRemainingRestriction()
    {
        var fixture = await OperationalConditionFixture.ReadyRoomAsync();
        var first = await fixture.CreateBlockingAsync("Klima soğutmuyor", OutageClassification.OutOfOrder);
        var second = await fixture.CreateBlockingAsync("Tesisat sızdırıyor", OutageClassification.OutOfService);
        var started = await fixture.Maintenance.Start.ExecuteAsync(
            new StartWorkCommand(first.Id, first.Version, fixture.Maintenance.ActorUserId),
            CancellationToken.None);

        var resolved = await fixture.Maintenance.Resolve.ExecuteAsync(
            new ResolveWorkCommand(
                started.Value!.Id,
                "Klima değişti.",
                PreparationImpact.None,
                started.Value.Version,
                fixture.Maintenance.ActorUserId),
            CancellationToken.None);

        Assert.True(resolved.IsSuccess);
        Assert.Equal(RoomReadiness.Ready, fixture.Room.CurrentReadiness);
        var detail = await fixture.Rooms.Detail.ExecuteAsync(fixture.RoomId, CancellationToken.None);
        Assert.Equal(RoomReadiness.Ready, detail.Value!.Readiness);
        Assert.Equal("OutOfService", detail.Value.TechnicalServiceability);
        Assert.True(detail.Value.HasActiveTechnicalIssue);
        Assert.Equal(second.Id, detail.Value.GoverningIssueId);
        Assert.Equal("Tesisat sızdırıyor", detail.Value.ActiveTechnicalIssueDescription);
    }

    [Fact]
    public async Task RoomOpsCaller_SeesOperationalCondition_ButNotProtectedIssueDetails()
    {
        var fixture = await OperationalConditionFixture.ReadyRoomAsync();
        await fixture.CreateBlockingAsync("Klima soğutmuyor", OutageClassification.OutOfOrder);
        var detail = await fixture.Rooms.Detail.ExecuteAsync(fixture.RoomId, CancellationToken.None);

        var exposed = RoomOperationsExposure.ForCaller(detail.Value!, canReadMaintenance: false);

        Assert.Equal(RoomReadiness.Ready, exposed.Readiness);
        Assert.Equal("OutOfOrder", exposed.TechnicalServiceability);
        Assert.True(exposed.HasActiveTechnicalIssue);
        Assert.Null(exposed.GoverningIssueId);
        Assert.Null(exposed.ActiveTechnicalIssueDescription);
        Assert.DoesNotContain(
            DevelopmentPersonaCatalog.RoomOperationsManager.Permissions,
            value => value.StartsWith("maintenance.", StringComparison.Ordinal));
        Assert.DoesNotContain(
            DevelopmentPersonaCatalog.RoomOperationsManager.Permissions,
            value => value == MaintenancePermissions.Read);
    }

    [Fact]
    public async Task MaintenanceReader_SeesGoverningIssueSummary()
    {
        var fixture = await OperationalConditionFixture.ReadyRoomAsync();
        var created = await fixture.CreateBlockingAsync("Klima soğutmuyor", OutageClassification.OutOfOrder);
        var detail = await fixture.Rooms.Detail.ExecuteAsync(fixture.RoomId, CancellationToken.None);

        var exposed = RoomOperationsExposure.ForCaller(detail.Value!, canReadMaintenance: true);

        Assert.Equal(created.Id, exposed.GoverningIssueId);
        Assert.Equal("Klima soğutmuyor", exposed.ActiveTechnicalIssueDescription);
        Assert.Contains(MaintenancePermissions.Read, DevelopmentPersonaCatalog.Broad(null).Permissions);
        Assert.Contains(RoomOperationsPermissions.Read, DevelopmentPersonaCatalog.Broad(null).Permissions);
    }

    [Fact]
    public void RoomReadiness_StillHasOnlyPreparationStates()
    {
        Assert.Equal(["Dirty", "Clean", "Inspected", "Ready"], Enum.GetNames<RoomReadiness>());
        Assert.Null(typeof(Room).GetProperty("RoomStatus"));
        Assert.Null(typeof(Room).GetProperty("Sellable"));
    }
}

internal sealed class OperationalConditionFixture
{
    public RoomOperationsHarness Rooms { get; }
    public TechnicalServiceHarness Maintenance { get; }
    public Guid RoomId { get; }
    public Room Room { get; }

    private OperationalConditionFixture(RoomOperationsHarness rooms, TechnicalServiceHarness maintenance, Room room)
    {
        Rooms = rooms;
        Maintenance = maintenance;
        Room = room;
        RoomId = room.Id;
        rooms.Serviceability.Inner = new TechnicalServiceRoomServiceabilityLookup(maintenance.Store);
    }

    public static Task<OperationalConditionFixture> ReadyRoomAsync()
    {
        var rooms = new RoomOperationsHarness();
        var maintenance = new TechnicalServiceHarness(rooms.PropertyId);
        var roomId = Guid.CreateVersion7();
        var room = rooms.SeedReadyRoom(roomId, "102");
        maintenance.AddRoom(roomId, "102");
        return Task.FromResult(new OperationalConditionFixture(rooms, maintenance, room));
    }

    public async Task<MaintenanceIssueDetail> CreateBlockingAsync(string description, OutageClassification outage)
    {
        var created = await Maintenance.Create.ExecuteAsync(
            Maintenance.CreateCommand(
                roomId: RoomId,
                description: description,
                assignedEmployeeId: Maintenance.EmployeeId,
                blocksRoomUse: true,
                outage: outage),
            CancellationToken.None);
        Assert.True(created.IsSuccess);
        return created.Value!;
    }
}
