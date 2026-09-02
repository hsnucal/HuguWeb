using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public sealed class PersonnelFinding03RegressionTests
{
    [Fact]
    public async Task Directory_IncludesActiveEmployee_WithoutEmployeeAccountLink()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(),
            CancellationToken.None);
        Assert.True(hired.IsSuccess, hired.Error?.Detail);

        var result = await harness.HrDirectory.ExecuteAsync(canReadSensitive: true, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Single(result.Value!);
        Assert.Equal(EmploymentStatus.Active, result.Value![0].EmploymentStatus);
    }

    [Fact]
    public async Task Directory_IncludesEmployee_WithCompletedOnboarding()
    {
        var harness = new WorkforceHarness();
        await harness.EnsurePersonnelEnrichmentDefaults.ExecuteAsync(
            harness.OrganizationId,
            CancellationToken.None);
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(),
            CancellationToken.None);
        Assert.True(hired.IsSuccess);

        var completed = await harness.CompleteEmploymentOnboarding.ExecuteAsync(
            hired.Value!.EmployeeId,
            CancellationToken.None);
        Assert.True(completed.IsSuccess, completed.Error?.Detail);

        var result = await harness.HrDirectory.ExecuteAsync(canReadSensitive: true, CancellationToken.None);
        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Single(result.Value!);
    }

    [Fact]
    public async Task Directory_IncludesEmployee_WithInProgressOnboarding()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(),
            CancellationToken.None);
        Assert.True(hired.IsSuccess);

        var checklist = await harness.OnboardingChecklist.ExecuteAsync(
            hired.Value!.EmployeeId,
            CancellationToken.None);
        Assert.True(checklist.IsSuccess);
        Assert.Equal(nameof(EmploymentOnboardingStatus.InProgress), checklist.Value!.OnboardingStatus);

        var result = await harness.HrDirectory.ExecuteAsync(canReadSensitive: true, CancellationToken.None);
        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Single(result.Value!);
    }

    [Fact]
    public async Task Directory_IncludesEmployee_WithDefaultFullTimeWorkType()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.HireWithProfile.ExecuteAsync(
            harness.HireWithProfileCommand(),
            CancellationToken.None);
        Assert.True(hired.IsSuccess);

        var employment = harness.Store.Employments.Single(item => item.Id == hired.Value!.EmploymentId);
        Assert.Equal(WorkType.FullTime, employment.WorkType);

        var result = await harness.HrDirectory.ExecuteAsync(canReadSensitive: true, CancellationToken.None);
        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Single(result.Value!);
    }

    [Fact]
    public async Task Departments_List_RequiresPropertyContext()
    {
        var harness = new WorkforceHarness(withoutPropertyContext: true);

        var result = await harness.Departments.ListAsync(CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("property-context-required", result.Error!.Code);
    }

    [Fact]
    public async Task Departments_List_ReturnsActiveDepartmentsForProperty()
    {
        var harness = new WorkforceHarness();

        var result = await harness.Departments.ListAsync(CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Contains(result.Value!, item => item.Id == harness.DepartmentId && item.IsActive);
    }
}
