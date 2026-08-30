using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class ScheduleWeekQueryTests
{
    private static readonly TimeOnly Eight = new(8, 0);
    private static readonly TimeOnly Sixteen = new(16, 0);
    private static readonly DateOnly WeekStart = new(2026, 8, 24); // Monday

    [Fact]
    public async Task Week_RequiresMondayStart()
    {
        var harness = new WorkforceHarness();
        var result = await harness.GetScheduleWeek.ExecuteAsync(
            new DateOnly(2026, 8, 25),
            departmentId: null,
            scopedPropertyId: null,
            allowedDepartmentIds: null,
            CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(ScheduleValidation.Codes.ScheduleWeekStartInvalid, result.Error!.Code);
    }

    [Fact]
    public async Task Week_ReturnsMondayToSundayDates()
    {
        var harness = new WorkforceHarness();
        var result = await harness.GetScheduleWeek.ExecuteAsync(
            WeekStart,
            departmentId: null,
            scopedPropertyId: null,
            allowedDepartmentIds: null,
            CancellationToken.None);
        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Equal(WeekStart, result.Value!.WeekStart);
        Assert.Equal(WeekStart.AddDays(6), result.Value.WeekEnd);
        Assert.Equal(7, result.Value.Dates.Count);
        Assert.Equal(DayOfWeek.Monday, result.Value.Dates[0].DayOfWeek);
        Assert.Equal(DayOfWeek.Sunday, result.Value.Dates[6].DayOfWeek);
    }

    [Fact]
    public async Task PropertyWide_IncludesEmployeesAcrossDepartments()
    {
        var harness = new WorkforceHarness();
        var (hkEmployee, _) = await harness.SeedEmploymentAsync(WeekStart.AddDays(-7));
        var fo = await harness.Hire.ExecuteAsync(
            harness.HireCommand(
                startDate: WeekStart.AddDays(-7),
                departmentId: harness.OtherDepartmentId,
                positionId: harness.OtherPositionId),
            CancellationToken.None);
        Assert.True(fo.IsSuccess, fo.Error?.Detail);

        var result = await harness.GetScheduleWeek.ExecuteAsync(
            WeekStart,
            departmentId: null,
            scopedPropertyId: harness.PropertyId,
            allowedDepartmentIds: null,
            CancellationToken.None);
        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.True(result.Value!.PropertyWide);
        Assert.Contains(result.Value.Employees, item => item.EmployeeId == hkEmployee);
        Assert.Contains(result.Value.Employees, item => item.EmployeeId == fo.Value!.EmployeeId);
    }

    [Fact]
    public async Task DepartmentFilter_ExcludesUnauthorizedDepartmentNamesFromFilter()
    {
        var harness = new WorkforceHarness();
        var allowed = new HashSet<Guid> { harness.DepartmentId };
        var result = await harness.GetScheduleWeek.ExecuteAsync(
            WeekStart,
            departmentId: null,
            scopedPropertyId: harness.PropertyId,
            allowed,
            CancellationToken.None);
        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.False(result.Value!.PropertyWide);
        Assert.All(result.Value.FilterDepartments, item => Assert.Equal(harness.DepartmentId, item.Id));
        Assert.DoesNotContain(result.Value.FilterDepartments, item => item.Id == harness.OtherDepartmentId);
    }

    [Fact]
    public async Task UnauthorizedDepartmentFilter_IsDenied()
    {
        var harness = new WorkforceHarness();
        var allowed = new HashSet<Guid> { harness.DepartmentId };
        var result = await harness.GetScheduleWeek.ExecuteAsync(
            WeekStart,
            departmentId: harness.OtherDepartmentId,
            scopedPropertyId: harness.PropertyId,
            allowed,
            CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(ScheduleValidation.Codes.ScheduleDepartmentFilterDenied, result.Error!.Code);
    }

    [Fact]
    public async Task TransferMidWeek_HousekeepingSeesOnlyAuthorizedCellsWithoutFoLeak()
    {
        var harness = new WorkforceHarness();
        var day = await CreateDayShiftAsync(harness);
        var (employeeId, _) = await harness.SeedEmploymentAsync(WeekStart.AddDays(-14));
        var transferDate = WeekStart.AddDays(3); // Thursday

        Assert.True((await harness.Transfer.ExecuteAsync(
            new TransferEmployeeCommand(
                employeeId,
                harness.OtherDepartmentId,
                harness.OtherPositionId,
                transferDate),
            CancellationToken.None)).IsSuccess);

        // FO schedule on Friday must not leak to HK-scoped week query.
        Assert.True((await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                employeeId,
                WeekStart.AddDays(4),
                ScheduleEntryKind.Shift,
                day.Id,
                null,
                "actor",
                harness.PropertyId,
                AllowedDepartmentIds: null),
            CancellationToken.None)).IsSuccess);

        var week = await harness.GetScheduleWeek.ExecuteAsync(
            WeekStart,
            departmentId: harness.DepartmentId,
            scopedPropertyId: harness.PropertyId,
            allowedDepartmentIds: new HashSet<Guid> { harness.DepartmentId },
            CancellationToken.None);
        Assert.True(week.IsSuccess, week.Error?.Detail);
        var row = Assert.Single(week.Value!.Employees, item => item.EmployeeId == employeeId);

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(ScheduleWeekCellDto.EligibilityEditable, row.Cells[i].Eligibility);
            Assert.Equal("Unscheduled", row.Cells[i].State);
            Assert.Equal(harness.DepartmentId, row.Cells[i].DepartmentId);
        }

        for (var i = 3; i < 7; i++)
        {
            Assert.Equal(ScheduleWeekCellDto.EligibilityOutOfScope, row.Cells[i].Eligibility);
            Assert.Null(row.Cells[i].State);
            Assert.Null(row.Cells[i].ShiftCode);
            Assert.Null(row.Cells[i].ScheduleEntryId);
        }
    }

    [Fact]
    public async Task EmploymentStartMidWeek_EarlierDaysNotEmployed()
    {
        var harness = new WorkforceHarness();
        var start = WeekStart.AddDays(2); // Wednesday
        var hired = await harness.Hire.ExecuteAsync(
            harness.HireCommand(startDate: start),
            CancellationToken.None);
        Assert.True(hired.IsSuccess, hired.Error?.Detail);

        var week = await harness.GetScheduleWeek.ExecuteAsync(
            WeekStart,
            departmentId: harness.DepartmentId,
            scopedPropertyId: null,
            allowedDepartmentIds: null,
            CancellationToken.None);
        Assert.True(week.IsSuccess, week.Error?.Detail);
        var row = Assert.Single(week.Value!.Employees, item => item.EmployeeId == hired.Value!.EmployeeId);
        Assert.Equal(ScheduleWeekCellDto.EligibilityNotEmployed, row.Cells[0].Eligibility);
        Assert.Equal(ScheduleWeekCellDto.EligibilityNotEmployed, row.Cells[1].Eligibility);
        Assert.Equal(ScheduleWeekCellDto.EligibilityEditable, row.Cells[2].Eligibility);
        Assert.Equal("Unscheduled", row.Cells[2].State);
    }

    [Fact]
    public async Task EmploymentEndMidWeek_LaterDaysNotEmployed()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync(WeekStart.AddDays(-14));
        var endDate = WeekStart.AddDays(4); // Friday
        Assert.True((await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(employeeId, endDate, EmploymentTerminationReason.Resignation),
            CancellationToken.None)).IsSuccess);

        var week = await harness.GetScheduleWeek.ExecuteAsync(
            WeekStart,
            departmentId: harness.DepartmentId,
            scopedPropertyId: null,
            allowedDepartmentIds: null,
            CancellationToken.None);
        Assert.True(week.IsSuccess, week.Error?.Detail);
        var row = Assert.Single(week.Value!.Employees, item => item.EmployeeId == employeeId);
        Assert.Equal(ScheduleWeekCellDto.EligibilityEditable, row.Cells[4].Eligibility);
        Assert.Equal(ScheduleWeekCellDto.EligibilityNotEmployed, row.Cells[5].Eligibility);
        Assert.Equal(ScheduleWeekCellDto.EligibilityNotEmployed, row.Cells[6].Eligibility);
        Assert.Null(row.Cells[5].State);
    }

    [Fact]
    public async Task Cells_ExposeShiftRestDayUnscheduled_AndInactiveHistoricalDefinition()
    {
        var harness = new WorkforceHarness();
        var day = await CreateDayShiftAsync(harness);
        var (employeeId, _) = await harness.SeedEmploymentAsync(WeekStart.AddDays(-7));

        Assert.True((await harness.UpsertSchedule.ExecuteAsync(
            ShiftCommand(employeeId, WeekStart, day.Id),
            CancellationToken.None)).IsSuccess);
        Assert.True((await harness.UpsertSchedule.ExecuteAsync(
            RestCommand(employeeId, WeekStart.AddDays(1)),
            CancellationToken.None)).IsSuccess);

        Assert.True((await harness.ShiftDefinitionAdmin.UpdateAsync(
            new UpdateShiftDefinitionCommand(
                day.Id,
                Name: null,
                StartLocalTime: null,
                EndLocalTime: null,
                EndsNextDay: null,
                BreakMinutes: null,
                IsActive: false,
                "actor"),
            CancellationToken.None)).IsSuccess);

        var week = await harness.GetScheduleWeek.ExecuteAsync(
            WeekStart,
            departmentId: harness.DepartmentId,
            scopedPropertyId: null,
            allowedDepartmentIds: null,
            CancellationToken.None);
        Assert.True(week.IsSuccess, week.Error?.Detail);
        var row = Assert.Single(week.Value!.Employees, item => item.EmployeeId == employeeId);
        Assert.Equal("Shift", row.Cells[0].State);
        Assert.Equal("sabah", row.Cells[0].ShiftCode);
        Assert.False(row.Cells[0].ShiftIsActive);
        Assert.Equal("RestDay", row.Cells[1].State);
        Assert.Equal("Unscheduled", row.Cells[2].State);
        Assert.DoesNotContain(
            week.Value.Employees.SelectMany(item => item.Cells),
            cell => cell.State is "Leave" or "PublicHoliday");
    }

    [Fact]
    public async Task FrontOfficeScoped_DoesNotSeeHousekeepingOnlyEmployee()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync(WeekStart.AddDays(-7));
        var week = await harness.GetScheduleWeek.ExecuteAsync(
            WeekStart,
            departmentId: harness.OtherDepartmentId,
            scopedPropertyId: harness.PropertyId,
            allowedDepartmentIds: new HashSet<Guid> { harness.OtherDepartmentId },
            CancellationToken.None);
        Assert.True(week.IsSuccess, week.Error?.Detail);
        Assert.DoesNotContain(week.Value!.Employees, item => item.EmployeeId == employeeId);
    }

    private static UpsertScheduleEntryCommand ShiftCommand(Guid employeeId, DateOnly date, Guid shiftDefinitionId) =>
        new(employeeId, date, ScheduleEntryKind.Shift, shiftDefinitionId, null, "actor", null);

    private static UpsertScheduleEntryCommand RestCommand(Guid employeeId, DateOnly date) =>
        new(employeeId, date, ScheduleEntryKind.RestDay, null, null, "actor", null);

    private static async Task<ShiftDefinitionDto> CreateDayShiftAsync(WorkforceHarness harness)
    {
        var created = await harness.ShiftDefinitionAdmin.CreateAsync(
            new CreateShiftDefinitionCommand("sabah", "Sabah", Eight, Sixteen, false, 30, "actor"),
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error?.Detail);
        return created.Value!;
    }
}
