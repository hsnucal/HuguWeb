using HuGuWeb.Api.Authorization;
using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class ScheduleSecurityTests
{
    private static readonly TimeOnly Eight = new(8, 0);
    private static readonly TimeOnly Sixteen = new(16, 0);

    private static Employee ForeignEmployee(WorkforceHarness harness)
    {
        Assert.True(Employee.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Foreign",
            "Person",
            "F-0001",
            out var employee,
            out _));
        harness.Store.Employees.Add(employee!);
        return employee!;
    }

    [Fact]
    public void PermissionCatalog_IncludesSchedulePermissions()
    {
        Assert.Contains(HrSchedulePermissions.Read, PermissionCatalog.All);
        Assert.Contains(HrSchedulePermissions.Manage, PermissionCatalog.All);
        Assert.Contains(HrShiftDefinitionPermissions.Read, PermissionCatalog.All);
        Assert.Contains(HrShiftDefinitionPermissions.Manage, PermissionCatalog.All);
        Assert.True(PermissionCatalog.IsKnown(HrSchedulePermissions.Read));
        Assert.True(PermissionCatalog.IsKnown(HrSchedulePermissions.Manage));
        Assert.Equal("hr", PermissionCatalog.DomainGroup(HrSchedulePermissions.Read));
        Assert.Equal("hr.schedule.read", HrSchedulePermissions.Read);
        Assert.Equal("hr.schedule.manage", HrSchedulePermissions.Manage);
    }

    [Fact]
    public void HrRoleTemplates_GrantSchedulePermissions()
    {
        Assert.Contains(HrSchedulePermissions.Read, SystemRoleTemplates.HumanResourcesPermissions);
        Assert.Contains(HrSchedulePermissions.Manage, SystemRoleTemplates.HumanResourcesPermissions);
        Assert.Contains(HrShiftDefinitionPermissions.Read, SystemRoleTemplates.HumanResourcesPermissions);
        Assert.Contains(HrShiftDefinitionPermissions.Manage, SystemRoleTemplates.HumanResourcesPermissions);
    }

    [Fact]
    public void Employee_HasNoUserIdProperty()
    {
        Assert.Null(typeof(Employee).GetProperty("UserId"));
        Assert.Null(typeof(Employee).GetProperty("ApplicationUserId"));
    }

    [Fact]
    public async Task Upsert_ScopedPropertyMismatch_IsDenied()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();
        var day = await CreateDayShiftAsync(harness);

        var result = await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                employeeId,
                harness.Clock.Today,
                ScheduleEntryKind.Shift,
                day.Id,
                Note: null,
                "actor",
                ScopedPropertyId: harness.OtherPropertyId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ScheduleValidation.Codes.SchedulePropertyAccessDenied, result.Error!.Code);
    }

    [Fact]
    public async Task GetScheduleState_ScopedPropertyMismatch_IsDenied()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();

        var result = await harness.GetScheduleState.ExecuteAsync(
            employeeId,
            harness.Clock.Today,
            scopedPropertyId: harness.OtherPropertyId,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ScheduleValidation.Codes.SchedulePropertyAccessDenied, result.Error!.Code);
    }

    [Fact]
    public async Task AfterTransfer_ScopedPropertyAccess_FollowsAssignmentProperty()
    {
        var harness = new WorkforceHarness();
        var start = harness.Clock.Today.AddDays(-20);
        var transferDate = harness.Clock.Today.AddDays(-5);
        var beforeDate = transferDate.AddDays(-2);
        var afterDate = transferDate.AddDays(2);

        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(startDate: start), CancellationToken.None);
        Assert.True(hired.IsSuccess, hired.Error?.Detail);
        var employeeId = hired.Value!.EmployeeId;

        var transfer = new TransferEmployeeUseCase(
            harness.Store,
            harness.Clock,
            new FixedWorkplace(harness.OrganizationId, harness.OtherPropertyId));
        var transferred = await transfer.ExecuteAsync(
            new TransferEmployeeCommand(
                employeeId,
                harness.OtherPropertyDepartmentId,
                harness.OtherPropertyPositionId,
                transferDate),
            CancellationToken.None);
        Assert.True(transferred.IsSuccess, transferred.Error?.Detail);

        var beforeOnA = await harness.GetScheduleState.ExecuteAsync(
            employeeId, beforeDate, harness.PropertyId, CancellationToken.None);
        Assert.True(beforeOnA.IsSuccess, beforeOnA.Error?.Detail);
        Assert.Equal(harness.PropertyId, beforeOnA.Value!.PropertyId);

        var beforeOnB = await harness.GetScheduleState.ExecuteAsync(
            employeeId, beforeDate, harness.OtherPropertyId, CancellationToken.None);
        Assert.False(beforeOnB.IsSuccess);
        Assert.Equal(ScheduleValidation.Codes.SchedulePropertyAccessDenied, beforeOnB.Error!.Code);

        var afterOnB = await harness.GetScheduleState.ExecuteAsync(
            employeeId, afterDate, harness.OtherPropertyId, CancellationToken.None);
        Assert.True(afterOnB.IsSuccess, afterOnB.Error?.Detail);
        Assert.Equal(harness.OtherPropertyId, afterOnB.Value!.PropertyId);

        var afterOnA = await harness.GetScheduleState.ExecuteAsync(
            employeeId, afterDate, harness.PropertyId, CancellationToken.None);
        Assert.False(afterOnA.IsSuccess);
        Assert.Equal(ScheduleValidation.Codes.SchedulePropertyAccessDenied, afterOnA.Error!.Code);
    }

    [Fact]
    public async Task ScheduleOps_ForEmployeeInAnotherOrganization_IsNotFound()
    {
        var harness = new WorkforceHarness();
        var foreign = ForeignEmployee(harness);
        var day = await CreateDayShiftAsync(harness);

        var upsert = await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                foreign.Id,
                harness.Clock.Today,
                ScheduleEntryKind.Shift,
                day.Id,
                Note: null,
                "actor",
                ScopedPropertyId: null),
            CancellationToken.None);
        Assert.False(upsert.IsSuccess);
        Assert.Equal("employee-not-found", upsert.Error!.Code);

        var state = await harness.GetScheduleState.ExecuteAsync(
            foreign.Id, harness.Clock.Today, null, CancellationToken.None);
        Assert.False(state.IsSuccess);
        Assert.Equal("employee-not-found", state.Error!.Code);

        var clear = await harness.ClearSchedule.ExecuteAsync(
            new ClearScheduleEntryCommand(foreign.Id, harness.Clock.Today, "actor", null),
            CancellationToken.None);
        Assert.False(clear.IsSuccess);
        Assert.Equal("employee-not-found", clear.Error!.Code);
    }

    private static async Task<ShiftDefinitionDto> CreateDayShiftAsync(WorkforceHarness harness)
    {
        var created = await harness.ShiftDefinitionAdmin.CreateAsync(
            new CreateShiftDefinitionCommand("DAY", "Day", Eight, Sixteen, false, 30, "actor"),
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error?.Detail);
        return created.Value!;
    }
}
