using HuGuWeb.RoomOperations.Domain;

namespace HuGuWeb.UnitTests.RoomOperations;

public class RoomReadinessTests
{
    [Fact]
    public void NewRoom_StartsDirty_NotReady()
    {
        Assert.True(Room.TryCreate(Guid.CreateVersion7(), Guid.CreateVersion7(), "101", Guid.CreateVersion7(), out var room, out _));

        Assert.Equal(RoomReadiness.Dirty, room!.CurrentReadiness);
        Assert.NotEqual(RoomReadiness.Ready, room.CurrentReadiness);
        Assert.Equal(4, Enum.GetValues<RoomReadiness>().Length);
        Assert.Equal(["Dirty", "Clean", "Inspected", "Ready"], Enum.GetNames<RoomReadiness>());
    }

    [Fact]
    public void Clean_CannotSkipInspectionToReady()
    {
        Assert.True(Room.TryCreate(Guid.CreateVersion7(), Guid.CreateVersion7(), "101", Guid.CreateVersion7(), out var room, out _));
        Assert.True(room!.TryMarkClean(room.ReadinessCycleId, out _));

        var result = room.TryMarkReady(out var error);

        Assert.False(result);
        Assert.Equal(RoomReadiness.Clean, room.CurrentReadiness);
        Assert.Contains("Inspected", error, StringComparison.Ordinal);
    }

    [Fact]
    public void Dirty_CannotBecomeInspected()
    {
        Assert.True(Room.TryCreate(Guid.CreateVersion7(), Guid.CreateVersion7(), "101", Guid.CreateVersion7(), out var room, out _));

        Assert.False(room!.TryMarkInspected(out _));
        Assert.Equal(RoomReadiness.Dirty, room.CurrentReadiness);
    }

    [Fact]
    public void AcceptedInspection_ProducesInspectedThenReady()
    {
        Assert.True(Room.TryCreate(Guid.CreateVersion7(), Guid.CreateVersion7(), "101", Guid.CreateVersion7(), out var room, out _));
        Assert.True(room!.TryMarkClean(room.ReadinessCycleId, out _));
        Assert.True(room.TryMarkInspected(out _));
        Assert.Equal(RoomReadiness.Inspected, room.CurrentReadiness);
        Assert.True(room.TryMarkReady(out _));
        Assert.Equal(RoomReadiness.Ready, room.CurrentReadiness);
    }

    [Fact]
    public void Rejection_RequiresReason()
    {
        var result = RoomInspection.TryReject(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            reason: "  ",
            DateTimeOffset.UtcNow,
            workItemId: null,
            out var inspection,
            out var error);

        Assert.False(result);
        Assert.Null(inspection);
        Assert.Equal("A rejection reason is required.", error);
    }
}
