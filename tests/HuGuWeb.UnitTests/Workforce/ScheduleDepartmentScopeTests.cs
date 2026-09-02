using HuGuWeb.Api.Authorization;
using HuGuWeb.Api.Identity;
using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class ScheduleDepartmentScopeTests
{
    private static readonly TimeOnly Eight = new(8, 0);
    private static readonly TimeOnly Sixteen = new(16, 0);

    private static async Task<ShiftDefinitionDto> CreateDayShiftAsync(WorkforceHarness harness)
    {
        var created = await harness.ShiftDefinitionAdmin.CreateAsync(
            new CreateShiftDefinitionCommand(
                "sabah",
                "Sabah",
                Eight,
                Sixteen,
                EndsNextDay: false,
                BreakMinutes: 30,
                "actor"),
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error?.Detail);
        return created.Value!;
    }

    [Fact]
    public void PermissionCatalog_IncludesShiftDefinitionPermissions()
    {
        Assert.Contains(HrShiftDefinitionPermissions.Read, PermissionCatalog.All);
        Assert.Contains(HrShiftDefinitionPermissions.Manage, PermissionCatalog.All);
        Assert.Equal("hr", PermissionCatalog.DomainGroup(HrShiftDefinitionPermissions.Read));
        Assert.Contains(HrShiftDefinitionPermissions.Read, SystemRoleTemplates.HumanResourcesPermissions);
        Assert.Contains(HrShiftDefinitionPermissions.Manage, SystemRoleTemplates.HumanResourcesPermissions);
        Assert.Contains(HrSchedulePermissions.Manage, SystemRoleTemplates.DepartmentSchedulerPermissions);
        Assert.Contains(HrShiftDefinitionPermissions.Read, SystemRoleTemplates.DepartmentSchedulerPermissions);
        Assert.DoesNotContain(
            HrShiftDefinitionPermissions.Manage,
            SystemRoleTemplates.DepartmentSchedulerPermissions);
        Assert.Equal(
            SystemRoleTemplates.DepartmentSchedulerOnlyPermissions,
            SystemRoleTemplates.ByCode(SystemRoleTemplates.DepartmentScheduler)!.Permissions);
        Assert.Contains(
            HrSchedulePermissions.Read,
            SystemRoleTemplates.DepartmentSchedulerOnlyPermissions);
        Assert.Contains(
            HrSchedulePermissions.Manage,
            SystemRoleTemplates.DepartmentSchedulerOnlyPermissions);
        Assert.Contains(
            HrShiftDefinitionPermissions.Read,
            SystemRoleTemplates.DepartmentSchedulerOnlyPermissions);
        Assert.DoesNotContain(
            HrLeavePermissions.Approve,
            SystemRoleTemplates.DepartmentSchedulerOnlyPermissions);
        Assert.Contains(
            SystemRoleTemplates.DepartmentScheduler,
            DevelopmentPersonaCatalog.MaintenanceManager.AssignedRoleCodes);
        Assert.Contains(
            HrSchedulePermissions.Manage,
            DevelopmentPersonaCatalog.MaintenanceManager.Permissions);
    }

    [Fact]
    public void ScheduleAccess_PropertyWide_AllowsAllDepartmentsInProperty()
    {
        var propertyId = Guid.CreateVersion7();
        var departmentA = Guid.CreateVersion7();
        var departmentB = Guid.CreateVersion7();

        Assert.True(ScheduleAccess.AllowsWorkplace(propertyId, null, propertyId, departmentA));
        Assert.True(ScheduleAccess.AllowsWorkplace(propertyId, null, propertyId, departmentB));
        Assert.False(ScheduleAccess.AllowsWorkplace(propertyId, null, Guid.CreateVersion7(), departmentA));
    }

    [Fact]
    public void ScheduleAccess_DepartmentLimited_FiltersDepartments()
    {
        var propertyId = Guid.CreateVersion7();
        var housekeeping = Guid.CreateVersion7();
        var frontOffice = Guid.CreateVersion7();
        var kitchen = Guid.CreateVersion7();
        var allowed = new HashSet<Guid> { housekeeping, frontOffice };

        Assert.True(ScheduleAccess.AllowsWorkplace(propertyId, allowed, propertyId, housekeeping));
        Assert.True(ScheduleAccess.AllowsWorkplace(propertyId, allowed, propertyId, frontOffice));
        Assert.False(ScheduleAccess.AllowsWorkplace(propertyId, allowed, propertyId, kitchen));
    }

    [Fact]
    public async Task PropertyWide_CanManageBothDepartments()
    {
        var harness = new WorkforceHarness();
        var day = await CreateDayShiftAsync(harness);
        var (employeeA, _) = await harness.SeedEmploymentAsync();
        var hiredB = await harness.Hire.ExecuteAsync(
            harness.HireCommand(departmentId: harness.OtherDepartmentId, positionId: harness.OtherPositionId),
            CancellationToken.None);
        Assert.True(hiredB.IsSuccess, hiredB.Error?.Detail);

        var a = await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                employeeA,
                harness.Clock.Today,
                ScheduleEntryKind.Shift,
                day.Id,
                null,
                "actor",
                harness.PropertyId,
                AllowedDepartmentIds: null),
            CancellationToken.None);
        var b = await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                hiredB.Value!.EmployeeId,
                harness.Clock.Today,
                ScheduleEntryKind.Shift,
                day.Id,
                null,
                "actor",
                harness.PropertyId,
                AllowedDepartmentIds: null),
            CancellationToken.None);

        Assert.True(a.IsSuccess, a.Error?.Detail);
        Assert.True(b.IsSuccess, b.Error?.Detail);
    }

    [Fact]
    public async Task HousekeepingOnly_CannotManageFrontOffice()
    {
        var harness = new WorkforceHarness();
        var day = await CreateDayShiftAsync(harness);
        var hired = await harness.Hire.ExecuteAsync(
            harness.HireCommand(departmentId: harness.OtherDepartmentId, positionId: harness.OtherPositionId),
            CancellationToken.None);
        Assert.True(hired.IsSuccess, hired.Error?.Detail);

        var result = await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                hired.Value!.EmployeeId,
                harness.Clock.Today,
                ScheduleEntryKind.Shift,
                day.Id,
                null,
                "actor",
                harness.PropertyId,
                AllowedDepartmentIds: new HashSet<Guid> { harness.DepartmentId }),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ScheduleValidation.Codes.SchedulePropertyAccessDenied, result.Error!.Code);
    }

    [Fact]
    public async Task HousekeepingOnly_CanManageHousekeeping()
    {
        var harness = new WorkforceHarness();
        var day = await CreateDayShiftAsync(harness);
        var (employeeId, _) = await harness.SeedEmploymentAsync();

        var result = await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                employeeId,
                harness.Clock.Today,
                ScheduleEntryKind.Shift,
                day.Id,
                null,
                "actor",
                harness.PropertyId,
                AllowedDepartmentIds: new HashSet<Guid> { harness.DepartmentId }),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
    }

    [Fact]
    public async Task MultiDepartment_AllowsAssignedDepartments_Only()
    {
        var harness = new WorkforceHarness();
        var day = await CreateDayShiftAsync(harness);
        var (housekeepingEmployee, _) = await harness.SeedEmploymentAsync();
        var frontOffice = await harness.Hire.ExecuteAsync(
            harness.HireCommand(departmentId: harness.OtherDepartmentId, positionId: harness.OtherPositionId),
            CancellationToken.None);
        Assert.True(frontOffice.IsSuccess, frontOffice.Error?.Detail);

        var allowed = new HashSet<Guid> { harness.DepartmentId, harness.OtherDepartmentId };
        var deniedThird = new HashSet<Guid> { Guid.CreateVersion7() };

        var okHk = await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                housekeepingEmployee,
                harness.Clock.Today,
                ScheduleEntryKind.RestDay,
                null,
                null,
                "actor",
                harness.PropertyId,
                allowed),
            CancellationToken.None);
        var okFo = await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                frontOffice.Value!.EmployeeId,
                harness.Clock.Today,
                ScheduleEntryKind.RestDay,
                null,
                null,
                "actor",
                harness.PropertyId,
                allowed),
            CancellationToken.None);
        var denied = await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                housekeepingEmployee,
                harness.Clock.Today.AddDays(1),
                ScheduleEntryKind.RestDay,
                null,
                null,
                "actor",
                harness.PropertyId,
                deniedThird),
            CancellationToken.None);

        Assert.True(okHk.IsSuccess, okHk.Error?.Detail);
        Assert.True(okFo.IsSuccess, okFo.Error?.Detail);
        Assert.False(denied.IsSuccess);
        Assert.Equal(ScheduleValidation.Codes.SchedulePropertyAccessDenied, denied.Error!.Code);
        _ = day;
    }

    [Fact]
    public async Task TransferHistorical_DepartmentScopeUsesTargetDateAssignment()
    {
        var harness = new WorkforceHarness();
        var day = await CreateDayShiftAsync(harness);
        var start = harness.Clock.Today.AddDays(-20);
        var transferDate = harness.Clock.Today.AddDays(-5);
        var before = transferDate.AddDays(-1);
        var after = transferDate;

        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(startDate: start), CancellationToken.None);
        Assert.True(hired.IsSuccess, hired.Error?.Detail);
        var transfer = await harness.Transfer.ExecuteAsync(
            new TransferEmployeeCommand(
                hired.Value!.EmployeeId,
                harness.OtherDepartmentId,
                harness.OtherPositionId,
                transferDate),
            CancellationToken.None);
        Assert.True(transfer.IsSuccess, transfer.Error?.Detail);

        var housekeepingOnly = new HashSet<Guid> { harness.DepartmentId };
        var frontOfficeOnly = new HashSet<Guid> { harness.OtherDepartmentId };

        var hkBefore = await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                hired.Value.EmployeeId,
                before,
                ScheduleEntryKind.Shift,
                day.Id,
                null,
                "actor",
                harness.PropertyId,
                housekeepingOnly),
            CancellationToken.None);
        var hkAfter = await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                hired.Value.EmployeeId,
                after,
                ScheduleEntryKind.Shift,
                day.Id,
                null,
                "actor",
                harness.PropertyId,
                housekeepingOnly),
            CancellationToken.None);
        var foBefore = await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                hired.Value.EmployeeId,
                before,
                ScheduleEntryKind.RestDay,
                null,
                null,
                "actor",
                harness.PropertyId,
                frontOfficeOnly),
            CancellationToken.None);
        var foAfter = await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                hired.Value.EmployeeId,
                after,
                ScheduleEntryKind.RestDay,
                null,
                null,
                "actor",
                harness.PropertyId,
                frontOfficeOnly),
            CancellationToken.None);

        Assert.True(hkBefore.IsSuccess, hkBefore.Error?.Detail);
        Assert.False(hkAfter.IsSuccess);
        Assert.Equal(ScheduleValidation.Codes.SchedulePropertyAccessDenied, hkAfter.Error!.Code);
        Assert.False(foBefore.IsSuccess);
        Assert.Equal(ScheduleValidation.Codes.SchedulePropertyAccessDenied, foBefore.Error!.Code);
        Assert.True(foAfter.IsSuccess, foAfter.Error?.Detail);
    }

    [Fact]
    public async Task RangeAcrossTransfer_DoesNotLeakUnauthorizedDepartmentRows()
    {
        var harness = new WorkforceHarness();
        var day = await CreateDayShiftAsync(harness);
        var start = harness.Clock.Today.AddDays(-20);
        var transferDate = harness.Clock.Today.AddDays(-5);
        var before = transferDate.AddDays(-1);
        var after = transferDate;

        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(startDate: start), CancellationToken.None);
        Assert.True(hired.IsSuccess);
        Assert.True((await harness.Transfer.ExecuteAsync(
            new TransferEmployeeCommand(
                hired.Value!.EmployeeId,
                harness.OtherDepartmentId,
                harness.OtherPositionId,
                transferDate),
            CancellationToken.None)).IsSuccess);

        Assert.True((await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                hired.Value.EmployeeId,
                before,
                ScheduleEntryKind.Shift,
                day.Id,
                null,
                "actor",
                harness.PropertyId),
            CancellationToken.None)).IsSuccess);
        Assert.True((await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                hired.Value.EmployeeId,
                after,
                ScheduleEntryKind.RestDay,
                null,
                null,
                "actor",
                harness.PropertyId),
            CancellationToken.None)).IsSuccess);

        var housekeepingRange = await harness.GetScheduleRange.ExecuteAsync(
            hired.Value.EmployeeId,
            before,
            after,
            harness.PropertyId,
            new HashSet<Guid> { harness.DepartmentId },
            CancellationToken.None);

        Assert.True(housekeepingRange.IsSuccess, housekeepingRange.Error?.Detail);
        Assert.Single(housekeepingRange.Value!);
        Assert.Equal(before, housekeepingRange.Value![0].ScheduleDate);
        Assert.Equal(harness.DepartmentId, housekeepingRange.Value[0].DepartmentId);
    }

    [Fact]
    public async Task ShiftDefinitionAdmin_RemainsPropertyScoped_IndependentOfDepartmentFilter()
    {
        var harness = new WorkforceHarness();
        var listed = await harness.ShiftDefinitionAdmin.ListAsync(false, CancellationToken.None);
        Assert.True(listed.IsSuccess);
        var created = await CreateDayShiftAsync(harness);
        Assert.Equal(harness.PropertyId, created.PropertyId);
    }
}
