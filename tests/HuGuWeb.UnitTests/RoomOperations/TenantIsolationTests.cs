using HuGuWeb.RoomOperations.Application;
using HuGuWeb.TechnicalService.Application;
using HuGuWeb.Workforce.Application;

namespace HuGuWeb.UnitTests.RoomOperations;

public class TenantIsolationTests
{
    [Fact]
    public async Task HotelA_CannotReadHotelB_Room()
    {
        var harness = new RoomOperationsHarness();
        var hotelB = harness.SeedRoom(Guid.CreateVersion7(), "201", harness.OtherPropertyId);

        var result = await harness.Detail.ExecuteAsync(hotelB.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("room-not-found", result.Error!.Code);
    }

    [Fact]
    public async Task HotelA_CannotMutateHotelB_Room()
    {
        var harness = new RoomOperationsHarness();
        var hotelB = harness.SeedRoom(Guid.CreateVersion7(), "202", harness.OtherPropertyId);

        var result = await harness.NeedsCleaning.ExecuteAsync(
            harness.NeedsCleaningCommand(hotelB.Id),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("room-not-found", result.Error!.Code);
    }

    [Fact]
    public async Task PropertyScopedCommand_WithoutProperty_IsRejected()
    {
        var harness = new RoomOperationsHarness();
        var query = new GetRoomOperationsDetailQuery(
            harness.Store,
            harness.Employees,
            harness.Serviceability,
            new FixedRoomWorkplace(Guid.Empty));

        var result = await query.ExecuteAsync(harness.RoomId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("property-context-required", result.Error!.Code);
    }
}
