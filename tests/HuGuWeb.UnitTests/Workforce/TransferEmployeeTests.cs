using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class TransferEmployeeTests
{
    [Fact]
    public async Task Transfer_EndsPreviousPrimaryOnDayBeforeAndStartsNewPrimaryOnD()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(
            harness.HireCommand(startDate: harness.Clock.Today.AddDays(-10)),
            CancellationToken.None);
        var effectiveDate = harness.Clock.Today;

        var result = await harness.Transfer.ExecuteAsync(
            new HuGuWeb.Workforce.Application.TransferEmployeeCommand(
                hired.Value!.EmployeeId,
                harness.OtherDepartmentId,
                harness.OtherPositionId,
                effectiveDate),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Equal(2, harness.Store.Assignments.Count);
        var previous = harness.Store.Assignments.Single(item => item.Id == result.Value!.ClosedAssignmentId);
        var next = harness.Store.Assignments.Single(item => item.Id == result.Value.NewAssignmentId);
        Assert.Equal(effectiveDate.AddDays(-1), previous.EndDate);
        Assert.Equal(effectiveDate, next.StartDate);
        Assert.Null(next.EndDate);
        Assert.Equal(harness.OtherDepartmentId, next.DepartmentId);
        Assert.Equal(harness.OtherPositionId, next.PositionId);
        Assert.False(PrimaryAssignments.HasOverlap(harness.Store.Assignments));
    }

    [Fact]
    public async Task Transfer_RetainsHistory()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);
        var employeeId = hired.Value!.EmployeeId;
        var originalAssignmentId = hired.Value.AssignmentId;

        await harness.Transfer.ExecuteAsync(
            new HuGuWeb.Workforce.Application.TransferEmployeeCommand(
                employeeId,
                harness.OtherDepartmentId,
                harness.OtherPositionId,
                harness.Clock.Today.AddDays(1)),
            CancellationToken.None);

        var history = await harness.History.ExecuteAsync(employeeId, CancellationToken.None);
        Assert.True(history.IsSuccess);
        Assert.Equal(2, history.Value!.Employments[0].PrimaryAssignments.Count);
        Assert.Contains(history.Value.Employments[0].PrimaryAssignments, item => item.Id == originalAssignmentId);
        Assert.Equal(2, harness.Store.Assignments.Count);
        Assert.Contains(harness.Store.Employees, item => item.Id == employeeId);
    }

    [Fact]
    public async Task Transfer_Overlap_IsPrevented()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);

        var result = await harness.Transfer.ExecuteAsync(
            new HuGuWeb.Workforce.Application.TransferEmployeeCommand(
                hired.Value!.EmployeeId,
                harness.OtherDepartmentId,
                harness.OtherPositionId,
                hired.Value.EmploymentStartDate),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid-transfer-date", result.Error!.Code);
        Assert.Single(harness.Store.Assignments);
        Assert.Null(harness.Store.Assignments[0].EndDate);
    }

    [Fact]
    public async Task Transfer_InactiveDestination_IsRejected()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);

        var result = await harness.Transfer.ExecuteAsync(
            new HuGuWeb.Workforce.Application.TransferEmployeeCommand(
                hired.Value!.EmployeeId,
                harness.InactiveDepartmentId,
                harness.PositionId,
                harness.Clock.Today.AddDays(1)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("department-inactive", result.Error!.Code);
    }

    [Fact]
    public async Task Transfer_InactivePosition_IsRejectedIndependentlyOfDepartment()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);

        var result = await harness.Transfer.ExecuteAsync(
            new HuGuWeb.Workforce.Application.TransferEmployeeCommand(
                hired.Value!.EmployeeId,
                harness.OtherDepartmentId,
                harness.InactivePositionId,
                harness.Clock.Today.AddDays(1)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("position-inactive", result.Error!.Code);
        Assert.Single(harness.Store.Assignments);
        Assert.Null(harness.Store.Assignments[0].EndDate);
    }

    [Fact]
    public async Task Transfer_UnknownDepartment_IsRejectedIndependentlyOfPosition()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);

        var result = await harness.Transfer.ExecuteAsync(
            new HuGuWeb.Workforce.Application.TransferEmployeeCommand(
                hired.Value!.EmployeeId,
                Guid.CreateVersion7(),
                harness.PositionId,
                harness.Clock.Today.AddDays(1)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("department-not-found", result.Error!.Code);
        Assert.Single(harness.Store.Assignments);
    }

    [Fact]
    public async Task Transfer_UnknownPosition_IsRejectedIndependentlyOfDepartment()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);

        var result = await harness.Transfer.ExecuteAsync(
            new HuGuWeb.Workforce.Application.TransferEmployeeCommand(
                hired.Value!.EmployeeId,
                harness.OtherDepartmentId,
                Guid.CreateVersion7(),
                harness.Clock.Today.AddDays(1)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("position-not-found", result.Error!.Code);
        Assert.Single(harness.Store.Assignments);
    }

    [Fact]
    public async Task Transfer_SamePositionToDifferentDepartment_KeepsPositionAndHistory()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(
            harness.HireCommand(startDate: harness.Clock.Today.AddDays(-10)),
            CancellationToken.None);
        var employeeId = hired.Value!.EmployeeId;
        var originalAssignmentId = hired.Value.AssignmentId;
        var effectiveDate = harness.Clock.Today;

        var result = await harness.Transfer.ExecuteAsync(
            new HuGuWeb.Workforce.Application.TransferEmployeeCommand(
                employeeId,
                harness.OtherDepartmentId,
                harness.PositionId,
                effectiveDate),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Equal(2, harness.Store.Assignments.Count);
        var previous = harness.Store.Assignments.Single(item => item.Id == originalAssignmentId);
        var next = harness.Store.Assignments.Single(item => item.Id == result.Value!.NewAssignmentId);
        Assert.Equal(harness.DepartmentId, previous.DepartmentId);
        Assert.Equal(harness.OtherDepartmentId, next.DepartmentId);
        Assert.Equal(harness.PositionId, previous.PositionId);
        Assert.Equal(harness.PositionId, next.PositionId);
        Assert.Equal(effectiveDate.AddDays(-1), previous.EndDate);
        Assert.Equal(effectiveDate, next.StartDate);
        Assert.Null(next.EndDate);

        var history = await harness.History.ExecuteAsync(employeeId, CancellationToken.None);
        Assert.True(history.IsSuccess);
        Assert.Equal(2, history.Value!.Employments[0].PrimaryAssignments.Count);
        Assert.Contains(history.Value.Employments[0].PrimaryAssignments, item => item.Id == originalAssignmentId);
        Assert.Equal(harness.OtherDepartmentId, history.Value.CurrentPrimaryAssignment!.DepartmentId);
        Assert.Equal(harness.PositionId, history.Value.CurrentPrimaryAssignment.PositionId);
    }

    [Fact]
    public async Task Transfer_EndedEmployment_IsRejected()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);
        Assert.True((await harness.EndEmployment.ExecuteAsync(
            new HuGuWeb.Workforce.Application.EndEmploymentCommand(
                hired.Value!.EmployeeId,
                harness.Clock.Today,
                EmploymentTerminationReason.Resignation),
            CancellationToken.None)).IsSuccess);

        var result = await harness.Transfer.ExecuteAsync(
            new HuGuWeb.Workforce.Application.TransferEmployeeCommand(
                hired.Value.EmployeeId,
                harness.OtherDepartmentId,
                harness.OtherPositionId,
                harness.Clock.Today),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("no-current-employment", result.Error!.Code);
    }
}
