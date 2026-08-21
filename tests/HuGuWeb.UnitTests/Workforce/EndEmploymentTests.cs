using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class EndEmploymentTests
{
    [Fact]
    public async Task EndEmployment_ClosesEmploymentAndAssignment_PreservesEmployeeAndHistory()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(
            harness.HireCommand(startDate: harness.Clock.Today.AddDays(-5)),
            CancellationToken.None);
        var employeeId = hired.Value!.EmployeeId;
        var assignmentId = hired.Value.AssignmentId;

        var result = await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(employeeId, harness.Clock.Today),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Equal(EmploymentStatus.Ended, result.Value!.Status);
        Assert.Equal(harness.Clock.Today, harness.Store.Employments[0].EndDate);
        Assert.Equal(harness.Clock.Today, harness.Store.Assignments[0].EndDate);
        Assert.Contains(harness.Store.Employees, item => item.Id == employeeId);
        Assert.Contains(harness.Store.Employments, item => item.Id == hired.Value.EmploymentId);
        Assert.Contains(harness.Store.Assignments, item => item.Id == assignmentId);

        var active = await harness.ActiveWorkforce.ExecuteAsync(CancellationToken.None);
        Assert.True(active.IsSuccess);
        Assert.DoesNotContain(active.Value!, item => item.EmployeeId == employeeId);

        var history = await harness.History.ExecuteAsync(employeeId, CancellationToken.None);
        Assert.True(history.IsSuccess);
        Assert.Equal(EmploymentStatus.Ended, history.Value!.Employments[0].Status);
        Assert.Equal(assignmentId, history.Value.Employments[0].PrimaryAssignments[0].Id);
    }

    [Fact]
    public async Task EndEmployment_InvalidEndDate_IsRejected()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(
            harness.HireCommand(startDate: harness.Clock.Today),
            CancellationToken.None);

        var result = await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(hired.Value!.EmployeeId, harness.Clock.Today.AddDays(-1)),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("invalid-employment-period", result.Error!.Code);
        Assert.False(harness.Store.Employments[0].IsEnded);
        Assert.Null(harness.Store.Assignments[0].EndDate);
        Assert.Single(harness.Store.Employees);
    }
}
