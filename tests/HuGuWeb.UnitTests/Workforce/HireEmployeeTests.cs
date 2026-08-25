using HuGuWeb.Workforce.Application;
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
    public async Task Hire_WithoutPropertyContext_IsRejected()
    {
        var harness = new WorkforceHarness();
        var useCase = new HireEmployeeUseCase(
            harness.Store,
            harness.Clock,
            new FixedWorkplace(harness.OrganizationId, Guid.Empty));

        var result = await useCase.ExecuteAsync(harness.HireCommand(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("property-context-required", result.Error!.Code);
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
    public async Task Hire_AssignsGeneratedPersonnelNumber()
    {
        var harness = new WorkforceHarness();

        var result = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PersonnelNumber.Format(PersonnelNumberSequence.StartingValue), result.Value!.PersonnelNumber);
        Assert.Equal(result.Value.PersonnelNumber, harness.Store.Employees[0].PersonnelNumber);
        Assert.NotEqual(result.Value.EmployeeId.ToString(), result.Value.PersonnelNumber);
    }

    [Fact]
    public async Task Hire_SubsequentHire_GetsNextPersonnelNumber()
    {
        var harness = new WorkforceHarness();
        var first = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);
        var second = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal("1001", first.Value!.PersonnelNumber);
        Assert.Equal("1002", second.Value!.PersonnelNumber);
        Assert.NotEqual(first.Value.PersonnelNumber, second.Value.PersonnelNumber);
    }

    [Fact]
    public async Task Hire_IgnoresCallerSuppliedPersonnelNumber_WhenPresentOnLegacyShape()
    {
        var harness = new WorkforceHarness();
        harness.SeedEmployee("P-1");

        var result = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Equal("1001", result.Value!.PersonnelNumber);
        Assert.Contains(harness.Store.Employees, item => item.PersonnelNumber == "P-1");
        Assert.Equal(2, harness.Store.Employees.Select(item => item.PersonnelNumber).Distinct().Count());
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
        Assert.Equal(["department-inactive"], result.Error.Errors![HrValidation.Fields.DepartmentId]);
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
        Assert.Equal(["position-inactive"], result.Error.Errors![HrValidation.Fields.PositionId]);
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
        Assert.Equal(["department-not-found"], result.Error.Errors![HrValidation.Fields.DepartmentId]);
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
        Assert.Equal(["position-not-found"], result.Error.Errors![HrValidation.Fields.PositionId]);
        Assert.Empty(harness.Store.Employees);
    }

    [Fact]
    public async Task Hire_CanUseSamePositionInAnyActiveDepartment_WhenApplicable()
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

    [Fact]
    public async Task Hire_PositionNotApplicableToDepartment_IsRejected()
    {
        var harness = new WorkforceHarness();

        var result = await harness.Hire.ExecuteAsync(
            harness.HireCommand(departmentId: harness.DepartmentId, positionId: harness.OtherPositionId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("position-not-available-for-department", result.Error!.Code);
        Assert.Equal(
            ["position-not-available-for-department"],
            result.Error.Errors![HrValidation.Fields.PositionId]);
        Assert.Empty(harness.Store.Employees);
    }
}
