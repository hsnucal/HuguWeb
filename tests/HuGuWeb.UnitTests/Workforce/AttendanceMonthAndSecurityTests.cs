using HuGuWeb.Api.Authorization;
using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class AttendanceMonthAndSecurityTests
{
    private static readonly TimeOnly Eight = new(8, 0);
    private static readonly TimeOnly Sixteen = new(16, 0);

    [Fact]
    public void PermissionCatalog_IncludesAttendancePermissions_CloseNotGrantedToHrOrScheduler()
    {
        Assert.Contains(HrAttendancePermissions.Read, PermissionCatalog.All);
        Assert.Contains(HrAttendancePermissions.Manage, PermissionCatalog.All);
        Assert.Contains(HrAttendancePermissions.Close, PermissionCatalog.All);
        Assert.Equal("hr", PermissionCatalog.DomainGroup(HrAttendancePermissions.Read));
        Assert.Contains(HrAttendancePermissions.Read, SystemRoleTemplates.HumanResourcesPermissions);
        Assert.Contains(HrAttendancePermissions.Manage, SystemRoleTemplates.HumanResourcesPermissions);
        Assert.DoesNotContain(HrAttendancePermissions.Close, SystemRoleTemplates.HumanResourcesPermissions);
        Assert.Contains(HrAttendancePermissions.Read, SystemRoleTemplates.DepartmentSchedulerOnlyPermissions);
        Assert.DoesNotContain(HrAttendancePermissions.Manage, SystemRoleTemplates.DepartmentSchedulerOnlyPermissions);
        Assert.DoesNotContain(HrAttendancePermissions.Close, SystemRoleTemplates.DepartmentSchedulerOnlyPermissions);
        Assert.Contains(HrAttendancePermissions.Close, PermissionCatalog.All);
        Assert.Contains(HrAttendancePermissions.Close, SystemRoleTemplates.ByCode(SystemRoleTemplates.DevelopmentSuperuser)!.Permissions);
    }

    [Fact]
    public async Task Month_RequiresPropertyContext()
    {
        var harness = new WorkforceHarness(withoutPropertyContext: true);
        var result = await harness.GetAttendanceMonth.ExecuteAsync(
            2026, 9, null, null, null, null, CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal("property-context-required", result.Error!.Code);
    }

    [Fact]
    public async Task Month_InvalidMonthRejected()
    {
        var harness = new WorkforceHarness();
        var result = await harness.GetAttendanceMonth.ExecuteAsync(
            2026, 13, null, null, harness.PropertyId, null, CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(AttendanceValidation.Codes.AttendanceInvalidMonth, result.Error!.Code);
    }

    [Fact]
    public async Task Month_IncludesMidMonthStartAndEnd_ExcludesNonOverlapping()
    {
        var harness = new WorkforceHarness();
        var starter = await harness.Hire.ExecuteAsync(
            new HireEmployeeCommand("Mehmet", "Kaya", new DateOnly(2026, 9, 20), harness.DepartmentId, harness.PositionId),
            CancellationToken.None);
        Assert.True(starter.IsSuccess, starter.Error?.Detail);

        var (endedEmployeeId, _) = await harness.SeedEmploymentAsync(new DateOnly(2026, 8, 1));
        var ended = await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(endedEmployeeId, new DateOnly(2026, 9, 15), EmploymentTerminationReason.Resignation),
            CancellationToken.None);
        Assert.True(ended.IsSuccess, ended.Error?.Detail);

        var outsider = await harness.Hire.ExecuteAsync(
            new HireEmployeeCommand("Zeynep", "Demir", new DateOnly(2026, 10, 1), harness.DepartmentId, harness.PositionId),
            CancellationToken.None);
        Assert.True(outsider.IsSuccess, outsider.Error?.Detail);

        var month = await QueryAsync(harness, 2026, 9);
        Assert.Equal(30, month.Dates.Count);
        Assert.Contains(month.Employees, item => item.EmployeeId == starter.Value!.EmployeeId);
        Assert.Contains(month.Employees, item => item.EmployeeId == endedEmployeeId);
        Assert.DoesNotContain(month.Employees, item => item.EmployeeId == outsider.Value!.EmployeeId);

        var startRow = month.Employees.Single(item => item.EmployeeId == starter.Value!.EmployeeId);
        Assert.Equal(nameof(AttendanceCoverage.NotEmployed), startRow.Days.Single(item => item.LocalDate == new DateOnly(2026, 9, 19)).Coverage);
        Assert.Equal(nameof(AttendanceCoverage.InEmployment), startRow.Days.Single(item => item.LocalDate == new DateOnly(2026, 9, 20)).Coverage);

        var endRow = month.Employees.Single(item => item.EmployeeId == endedEmployeeId);
        Assert.Equal(nameof(AttendanceCoverage.InEmployment), endRow.Days.Single(item => item.LocalDate == new DateOnly(2026, 9, 15)).Coverage);
        Assert.Equal(nameof(AttendanceCoverage.NotEmployed), endRow.Days.Single(item => item.LocalDate == new DateOnly(2026, 9, 16)).Coverage);
    }

    [Fact]
    public async Task Month_DepartmentFilterAndSearch()
    {
        var harness = new WorkforceHarness();
        var hk = await harness.Hire.ExecuteAsync(
            new HireEmployeeCommand("Ayşe", "Yılmaz", new DateOnly(2026, 9, 1), harness.DepartmentId, harness.PositionId),
            CancellationToken.None);
        var fo = await harness.Hire.ExecuteAsync(
            new HireEmployeeCommand("Mehmet", "Kaya", new DateOnly(2026, 9, 1), harness.OtherDepartmentId, harness.OtherPositionId),
            CancellationToken.None);
        Assert.True(hk.IsSuccess, hk.Error?.Detail);
        Assert.True(fo.IsSuccess, fo.Error?.Detail);

        var filtered = await QueryAsync(harness, 2026, 9, departmentId: harness.DepartmentId);
        Assert.Contains(filtered.Employees, item => item.EmployeeId == hk.Value!.EmployeeId);
        Assert.DoesNotContain(filtered.Employees, item => item.EmployeeId == fo.Value!.EmployeeId);

        var searched = await QueryAsync(harness, 2026, 9, search: "Mehmet");
        Assert.Contains(searched.Employees, item => item.EmployeeId == fo.Value!.EmployeeId);
        Assert.DoesNotContain(searched.Employees, item => item.EmployeeId == hk.Value!.EmployeeId);
    }

    [Fact]
    public async Task Month_UnauthorizedDepartmentFilter_IsDenied()
    {
        var harness = new WorkforceHarness();
        var result = await harness.GetAttendanceMonth.ExecuteAsync(
            2026,
            9,
            harness.OtherDepartmentId,
            null,
            harness.PropertyId,
            new HashSet<Guid> { harness.DepartmentId },
            CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(AttendanceValidation.Codes.AttendanceDepartmentFilterDenied, result.Error!.Code);
    }

    [Fact]
    public async Task Month_DepartmentScope_HidesOtherDepartmentEmployees()
    {
        var harness = new WorkforceHarness();
        var hk = await harness.SeedEmploymentAsync(new DateOnly(2026, 9, 1));
        var fo = await harness.Hire.ExecuteAsync(
            new HireEmployeeCommand("Mehmet", "Kaya", new DateOnly(2026, 9, 1), harness.OtherDepartmentId, harness.OtherPositionId),
            CancellationToken.None);
        Assert.True(fo.IsSuccess, fo.Error?.Detail);

        var result = await harness.GetAttendanceMonth.ExecuteAsync(
            2026,
            9,
            null,
            null,
            harness.PropertyId,
            new HashSet<Guid> { harness.DepartmentId },
            CancellationToken.None);
        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.False(result.Value!.PropertyWide);
        Assert.Contains(result.Value.Employees, item => item.EmployeeId == hk.EmployeeId);
        Assert.DoesNotContain(result.Value.Employees, item => item.EmployeeId == fo.Value!.EmployeeId);
    }

    [Fact]
    public async Task Month_TotalsAndSourcePrecedenceAcrossDays()
    {
        var harness = new WorkforceHarness();
        var leaveType = harness.SeedLeaveType("annual", "Annual", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, employmentId) = await harness.SeedEmploymentAsync(new DateOnly(2026, 9, 1));
        var shift = await harness.ShiftDefinitionAdmin.CreateAsync(
            new CreateShiftDefinitionCommand("DAY", "Day", Eight, Sixteen, false, 30, "actor"),
            CancellationToken.None);
        Assert.True(shift.IsSuccess, shift.Error?.Detail);

        Assert.True((await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                employeeId, new DateOnly(2026, 9, 1), ScheduleEntryKind.Shift, shift.Value!.Id, null, "actor", harness.PropertyId),
            CancellationToken.None)).IsSuccess);
        Assert.True((await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                employeeId, new DateOnly(2026, 9, 2), ScheduleEntryKind.RestDay, null, null, "actor", harness.PropertyId),
            CancellationToken.None)).IsSuccess);
        Assert.True((await harness.RecordLeave.ExecuteAsync(
            new RecordLeaveCommand(employeeId, null, leaveType.Id, new DateOnly(2026, 9, 3), new DateOnly(2026, 9, 3), 1.0m, null, "actor"),
            CancellationToken.None)).IsSuccess);
        Assert.True((await harness.SetAttendanceCorrection.ExecuteAsync(
            new SetAttendanceCorrectionCommand(
                employmentId, new DateOnly(2026, 9, 4), "Absent", "no show", "actor", harness.PropertyId),
            CancellationToken.None)).IsSuccess);

        var month = await QueryAsync(harness, 2026, 9);
        var row = Assert.Single(month.Employees, item => item.EmployeeId == employeeId);
        Assert.Equal(1, row.Totals.WorkedDays);
        Assert.Equal(1, row.Totals.RestDays);
        Assert.Equal(1, row.Totals.LeaveDays);
        Assert.Equal(1, row.Totals.AbsentDays);
        Assert.Equal(26, row.Totals.UnresolvedDays);
        Assert.Equal(shift.Value.PlannedNetMinutes, row.Totals.PlannedMinutes);
        Assert.Equal(nameof(AttendanceSource.Schedule), row.Days.Single(item => item.LocalDate == new DateOnly(2026, 9, 1)).Source);
        Assert.Equal(nameof(AttendanceSource.Leave), row.Days.Single(item => item.LocalDate == new DateOnly(2026, 9, 3)).Source);
        Assert.Equal(nameof(AttendanceSource.Manual), row.Days.Single(item => item.LocalDate == new DateOnly(2026, 9, 4)).Source);
    }

    [Fact]
    public async Task WrongProperty_IsDenied()
    {
        var harness = new WorkforceHarness();
        var (_, employmentId) = await harness.SeedEmploymentAsync(new DateOnly(2026, 9, 1));
        var result = await harness.SetAttendanceCorrection.ExecuteAsync(
            new SetAttendanceCorrectionCommand(
                employmentId,
                new DateOnly(2026, 9, 10),
                "Absent",
                "no show",
                "actor",
                harness.OtherPropertyId),
            CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(AttendanceValidation.Codes.AttendancePropertyAccessDenied, result.Error!.Code);
    }

    [Fact]
    public async Task WrongOrganization_IsNotFound()
    {
        var harness = new WorkforceHarness();
        Assert.True(Employee.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "Foreign",
            "Person",
            "F-0001",
            out var employee,
            out _));
        harness.Store.Employees.Add(employee!);
        var employment = Employment.Open(Guid.CreateVersion7(), employee!.Id, new DateOnly(2026, 9, 1), harness.Clock.Today);
        harness.Store.Employments.Add(employment);

        var result = await harness.SetAttendanceCorrection.ExecuteAsync(
            new SetAttendanceCorrectionCommand(
                employment.Id,
                new DateOnly(2026, 9, 10),
                "Absent",
                "no show",
                "actor",
                harness.PropertyId),
            CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(AttendanceValidation.Codes.AttendanceEmploymentNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task DepartmentScope_DeniedForUnauthorizedDepartment()
    {
        var harness = new WorkforceHarness();
        var (_, employmentId) = await harness.SeedEmploymentAsync(new DateOnly(2026, 9, 1));
        var result = await harness.SetAttendanceCorrection.ExecuteAsync(
            new SetAttendanceCorrectionCommand(
                employmentId,
                new DateOnly(2026, 9, 10),
                "Absent",
                "no show",
                "actor",
                harness.PropertyId,
                new HashSet<Guid> { harness.OtherDepartmentId }),
            CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(AttendanceValidation.Codes.AttendanceDepartmentScopeDenied, result.Error!.Code);
    }

    [Fact]
    public async Task MissingEmployment_IsNotFound()
    {
        var harness = new WorkforceHarness();
        var result = await harness.SetAttendanceCorrection.ExecuteAsync(
            new SetAttendanceCorrectionCommand(
                Guid.CreateVersion7(),
                new DateOnly(2026, 9, 10),
                "Absent",
                "no show",
                "actor",
                harness.PropertyId),
            CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(AttendanceValidation.Codes.AttendanceEmploymentNotFound, result.Error!.Code);
    }

    private static async Task<AttendanceMonthDto> QueryAsync(
        WorkforceHarness harness,
        int year,
        int month,
        Guid? departmentId = null,
        string? search = null)
    {
        var result = await harness.GetAttendanceMonth.ExecuteAsync(
            year,
            month,
            departmentId,
            search,
            harness.PropertyId,
            allowedDepartmentIds: null,
            CancellationToken.None);
        Assert.True(result.IsSuccess, result.Error?.Detail);
        return result.Value!;
    }
}
