using HuGuWeb.TechnicalService.Application;

namespace HuGuWeb.UnitTests.TechnicalService;

public class TenantIsolationTests
{
    [Fact]
    public async Task HotelA_CannotReadHotelB_Issue()
    {
        var harness = new TechnicalServiceHarness();
        var created = await harness.Create.ExecuteAsync(harness.CreateCommand(), CancellationToken.None);
        Assert.True(created.IsSuccess);

        var hotelB = new GetIssueDetailQuery(
            harness.Store,
            harness.Employees,
            harness.Rooms,
            new FixedTechnicalServiceWorkplace(harness.OtherPropertyId));
        var result = await hotelB.ExecuteAsync(created.Value!.Id, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("issue-not-found", result.Error!.Code);
    }

    [Fact]
    public async Task HotelA_CannotMutateHotelB_Issue()
    {
        var harness = new TechnicalServiceHarness();
        var created = await harness.Create.ExecuteAsync(harness.CreateCommand(), CancellationToken.None);
        Assert.True(created.IsSuccess);

        var hotelB = new AssignIssueUseCase(
            harness.Store,
            harness.Employees,
            harness.Rooms,
            new FixedTechnicalServiceWorkplace(harness.OtherPropertyId),
            harness.Clock);
        var result = await hotelB.ExecuteAsync(
            new AssignIssueCommand(created.Value!.Id, harness.EmployeeId, created.Value.Version, harness.ActorUserId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("issue-not-found", result.Error!.Code);
    }

    [Fact]
    public async Task PropertyScopedCommand_WithoutProperty_IsRejected()
    {
        var harness = new TechnicalServiceHarness();
        var query = new GetIssueDetailQuery(
            harness.Store,
            harness.Employees,
            harness.Rooms,
            new FixedTechnicalServiceWorkplace(Guid.Empty));

        var result = await query.ExecuteAsync(Guid.CreateVersion7(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("property-context-required", result.Error!.Code);
    }
}
