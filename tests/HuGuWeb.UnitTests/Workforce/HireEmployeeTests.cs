using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class HireEmployeeTests
{
    [Fact]
    public async Task Hire_CreatesEmployeeEmploymentAndPrimaryAssignment()
    {
        var harness = new WorkforceHarness();

        var result = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(harness.Store.Employees);
        Assert.Single(harness.Store.Employments);
        Assert.Single(harness.Store.Assignments);
        Assert.Equal(EmploymentStatus.Active, result.Value!.EmploymentStatus);
        Assert.Equal(harness.Store.Employees[0].Id, harness.Store.Employments[0].EmployeeId);
        Assert.Equal(harness.Store.Employments[0].Id, harness.Store.Assignments[0].EmploymentId);
        Assert.Equal(AssignmentKind.Primary, harness.Store.Assignments[0].Kind);
        Assert.Null(harness.Store.Assignments[0].EndDate);
    }

    [Fact]
    public async Task Hire_FutureStart_IsScheduled()
    {
        var harness = new WorkforceHarness();
        var start = harness.Clock.Today.AddDays(10);

        var result = await harness.Hire.ExecuteAsync(harness.HireCommand(startDate: start), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(EmploymentStatus.Scheduled, result.Value!.EmploymentStatus);
        Assert.Equal(EmploymentStatus.Scheduled, harness.Store.Employments[0].EffectiveStatus(harness.Clock.Today));
    }

    [Fact]
    public async Task Hire_DuplicatePersonnelNumber_IsRejected()
    {
        var harness = new WorkforceHarness();
        Assert.True((await harness.Hire.ExecuteAsync(harness.HireCommand("P-1"), CancellationToken.None)).IsSuccess);

        var result = await harness.Hire.ExecuteAsync(harness.HireCommand("P-1"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("personnel-number-in-use", result.Error!.Code);
        Assert.Single(harness.Store.Employees);
    }

    [Fact]
    public async Task Hire_InactiveDepartment_IsRejected()
    {
        var harness = new WorkforceHarness();

        var result = await harness.Hire.ExecuteAsync(
            harness.HireCommand(departmentId: harness.InactiveDepartmentId, positionId: harness.PositionId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("department-inactive", result.Error!.Code);
        Assert.Empty(harness.Store.Employees);
    }

    [Fact]
    public async Task Hire_InactivePosition_IsRejected()
    {
        var harness = new WorkforceHarness();

        var result = await harness.Hire.ExecuteAsync(
            harness.HireCommand(positionId: harness.InactivePositionId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("position-inactive", result.Error!.Code);
        Assert.Empty(harness.Store.Employees);
    }

    [Fact]
    public async Task Hire_DoesNotCreateApplicationUser()
    {
        var harness = new WorkforceHarness();

        var result = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(typeof(Employee).GetProperty("UserId"));
        Assert.Null(typeof(Employee).GetProperty("ApplicationUserId"));
    }

    [Fact]
    public async Task Hire_UnknownDepartment_IsRejectedIndependentlyOfPosition()
    {
        var harness = new WorkforceHarness();

        var result = await harness.Hire.ExecuteAsync(
            harness.HireCommand(departmentId: Guid.CreateVersion7(), positionId: harness.PositionId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("department-not-found", result.Error!.Code);
        Assert.Empty(harness.Store.Employees);
    }

    [Fact]
    public async Task Hire_UnknownPosition_IsRejectedIndependentlyOfDepartment()
    {
        var harness = new WorkforceHarness();

        var result = await harness.Hire.ExecuteAsync(
            harness.HireCommand(departmentId: harness.DepartmentId, positionId: Guid.CreateVersion7()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("position-not-found", result.Error!.Code);
        Assert.Empty(harness.Store.Employees);
    }

    [Fact]
    public async Task Hire_CanUseSamePositionInAnyActiveDepartment()
    {
        var harness = new WorkforceHarness();

        var result = await harness.Hire.ExecuteAsync(
            harness.HireCommand(departmentId: harness.OtherDepartmentId, positionId: harness.PositionId),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Equal(harness.OtherDepartmentId, result.Value!.DepartmentId);
        Assert.Equal(harness.PositionId, result.Value.PositionId);
        Assert.Equal(harness.OtherDepartmentId, harness.Store.Assignments[0].DepartmentId);
        Assert.Equal(harness.PositionId, harness.Store.Assignments[0].PositionId);
    }
}
