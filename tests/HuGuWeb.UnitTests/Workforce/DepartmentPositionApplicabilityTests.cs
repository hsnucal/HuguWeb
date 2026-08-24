using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class DepartmentPositionApplicabilityTests
{
    [Fact]
    public void Position_RemainsPropertyScoped_WithoutDepartmentId()
    {
        var propertyId = Guid.CreateVersion7();
        Assert.True(Position.TryCreate(Guid.CreateVersion7(), propertyId, "Uzman", "SPEC", out var position, out _));
        Assert.NotNull(position);
        Assert.Equal(propertyId, position.PropertyId);
        Assert.Null(typeof(Position).GetProperty("DepartmentId"));
        Assert.Null(typeof(DepartmentPositionApplicability).GetProperty("Permission"));
        Assert.Null(typeof(DepartmentPositionApplicability).GetProperty("Permissions"));
        Assert.Equal(nameof(DepartmentPositionApplicability.DepartmentId), "DepartmentId");
        Assert.Equal(nameof(DepartmentPositionApplicability.PositionId), "PositionId");
    }

    [Fact]
    public async Task OnePosition_CanBeApplicableToMultipleDepartments()
    {
        var harness = new WorkforceHarness();
        var first = await harness.Hire.ExecuteAsync(
            harness.HireCommand(departmentId: harness.DepartmentId, positionId: harness.PositionId),
            CancellationToken.None);
        var second = await harness.Hire.ExecuteAsync(
            harness.HireCommand(departmentId: harness.OtherDepartmentId, positionId: harness.PositionId),
            CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error?.Detail);
        Assert.True(second.IsSuccess, second.Error?.Detail);
        Assert.Equal(harness.PositionId, first.Value!.PositionId);
        Assert.Equal(harness.PositionId, second.Value!.PositionId);
        Assert.Single(harness.Store.Positions, item => item.Id == harness.PositionId);
        Assert.Equal(2, harness.Store.Applicabilities.Count(item => item.PositionId == harness.PositionId));
    }

    [Fact]
    public async Task DepartmentFilters_OnlyApplicablePositions()
    {
        var harness = new WorkforceHarness();
        var positions = new MaintainPositionsUseCase(harness.Store, harness.Workplace);
        var listed = await positions.ListAsync(CancellationToken.None);

        Assert.True(listed.IsSuccess);
        var attendant = listed.Value!.Single(item => item.Id == harness.PositionId);
        var receptionist = listed.Value.Single(item => item.Id == harness.OtherPositionId);
        Assert.Contains(harness.DepartmentId, attendant.ApplicableDepartmentIds);
        Assert.Contains(harness.OtherDepartmentId, attendant.ApplicableDepartmentIds);
        Assert.DoesNotContain(harness.DepartmentId, receptionist.ApplicableDepartmentIds);
        Assert.Contains(harness.OtherDepartmentId, receptionist.ApplicableDepartmentIds);
    }

    [Fact]
    public async Task InvalidDepartmentPositionHire_IsRejected()
    {
        var harness = new WorkforceHarness();

        var result = await harness.Hire.ExecuteAsync(
            harness.HireCommand(departmentId: harness.DepartmentId, positionId: harness.OtherPositionId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("position-not-available-for-department", result.Error!.Code);
    }

    [Fact]
    public async Task InactiveDepartment_IsRejectedBeforeApplicability()
    {
        var harness = new WorkforceHarness();
        harness.AddApplicability(harness.InactiveDepartmentId, harness.PositionId);

        var result = await harness.Hire.ExecuteAsync(
            harness.HireCommand(departmentId: harness.InactiveDepartmentId, positionId: harness.PositionId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("department-inactive", result.Error!.Code);
    }

    [Fact]
    public async Task InactivePosition_IsRejectedBeforeApplicability()
    {
        var harness = new WorkforceHarness();
        harness.AddApplicability(harness.DepartmentId, harness.InactivePositionId);

        var result = await harness.Hire.ExecuteAsync(
            harness.HireCommand(positionId: harness.InactivePositionId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("position-inactive", result.Error!.Code);
    }

    [Fact]
    public async Task Transfer_RejectsInvalidApplicability()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);

        var result = await harness.Transfer.ExecuteAsync(
            new TransferEmployeeCommand(
                hired.Value!.EmployeeId,
                harness.DepartmentId,
                harness.OtherPositionId,
                harness.Clock.Today.AddDays(1)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("position-not-available-for-department", result.Error!.Code);
        Assert.Single(harness.Store.Assignments);
    }

    [Fact]
    public async Task Transfer_AllowsSharedPositionAcrossDepartments()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(
            harness.HireCommand(startDate: harness.Clock.Today.AddDays(-10)),
            CancellationToken.None);

        var result = await harness.Transfer.ExecuteAsync(
            new TransferEmployeeCommand(
                hired.Value!.EmployeeId,
                harness.OtherDepartmentId,
                harness.PositionId,
                harness.Clock.Today),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Equal(harness.PositionId, result.Value!.PositionId);
        Assert.Equal(harness.OtherDepartmentId, result.Value.DepartmentId);
    }

    [Fact]
    public async Task ChangingApplicability_DoesNotGrantPermissions()
    {
        var harness = new WorkforceHarness();
        var maintain = new MaintainPositionsUseCase(harness.Store, harness.Workplace);
        var created = await maintain.CreateAsync(
            new CreatePositionCommand("Paylaşılan", "SHARE", [harness.DepartmentId, harness.OtherDepartmentId]),
            CancellationToken.None);

        Assert.True(created.IsSuccess, created.Error?.Detail);
        Assert.Equal(2, created.Value!.ApplicableDepartmentIds.Count);
        Assert.Null(typeof(Position).GetProperty("Permission"));
        Assert.Null(typeof(DepartmentPositionApplicability).GetProperty("Role"));
        Assert.DoesNotContain(
            typeof(DepartmentPositionApplicability).Assembly.GetReferencedAssemblies().Select(name => name.Name),
            name => name == "HuGuWeb.Api");
    }

    [Fact]
    public void ClearingInvalidPosition_WhenDepartmentChanges()
    {
        var selectedPosition = Guid.CreateVersion7();
        var housekeeping = Guid.CreateVersion7();
        var frontOffice = Guid.CreateVersion7();
        var applicable = new Dictionary<Guid, Guid[]>
        {
            [housekeeping] = [selectedPosition],
            [frontOffice] = [Guid.CreateVersion7()]
        };

        var nextDepartment = frontOffice;
        var retained = applicable[nextDepartment].Contains(selectedPosition) ? selectedPosition : Guid.Empty;

        Assert.Equal(Guid.Empty, retained);
    }
}
