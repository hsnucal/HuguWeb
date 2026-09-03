using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class AttendanceResolutionTests
{
    private static readonly TimeOnly Eight = new(8, 0);
    private static readonly TimeOnly Sixteen = new(16, 0);
    private static readonly DateOnly Day = new(2026, 9, 10);

    [Fact]
    public async Task ScheduledShift_ResolvesProvisionalWorkedFromSchedule()
    {
        var harness = new WorkforceHarness();
        var (employeeId, employmentId) = await harness.SeedEmploymentAsync(Day.AddDays(-5));
        var shift = await CreateDayShiftAsync(harness);
        await UpsertShiftAsync(harness, employeeId, Day, shift.Id);

        var month = await QuerySeptemberAsync(harness);
        var cell = DayOf(month, employeeId, Day);
        Assert.Equal(nameof(AttendanceCoverage.InEmployment), cell.Coverage);
        Assert.Equal(nameof(AttendanceAcceptedKind.Worked), cell.AcceptedKind);
        Assert.Equal(nameof(AttendanceSource.Schedule), cell.Source);
        Assert.True(cell.IsProvisional);
        Assert.False(cell.IsManual);
        Assert.False(cell.IsUnresolved);
        Assert.Equal("Shift", cell.Schedule!.State);
        Assert.Equal(shift.Code, cell.Schedule.ShiftCode);
        Assert.Equal(Eight, cell.Schedule.StartLocalTime);
        Assert.Equal(Sixteen, cell.Schedule.EndLocalTime);
        Assert.Equal(shift.PlannedNetMinutes, cell.PlannedMinutes);
        Assert.Null(cell.AcceptedWorkedMinutes);
        Assert.Equal(employmentId, cell.EmploymentId);
        Assert.Single(harness.Store.ScheduleEntries);
    }

    [Fact]
    public async Task RestDay_ResolvesRestDayFromSchedule()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync(Day.AddDays(-5));
        await UpsertRestAsync(harness, employeeId, Day);

        var cell = DayOf(await QuerySeptemberAsync(harness), employeeId, Day);
        Assert.Equal(nameof(AttendanceAcceptedKind.RestDay), cell.AcceptedKind);
        Assert.Equal(nameof(AttendanceSource.Schedule), cell.Source);
        Assert.False(cell.IsProvisional);
        Assert.Equal("RestDay", cell.Schedule!.State);
        Assert.Null(cell.AcceptedWorkedMinutes);
    }

    [Fact]
    public async Task NoSchedule_ResolvesUnresolved_NeverAbsent()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync(Day.AddDays(-5));

        var cell = DayOf(await QuerySeptemberAsync(harness), employeeId, Day);
        Assert.Equal(nameof(AttendanceAcceptedKind.Unresolved), cell.AcceptedKind);
        Assert.Null(cell.Source);
        Assert.True(cell.IsUnresolved);
        Assert.NotEqual(nameof(AttendanceAcceptedKind.Absent), cell.AcceptedKind);
        Assert.Equal("Unscheduled", cell.Schedule!.State);
    }

    [Fact]
    public async Task RecordedLeave_OverridesScheduledShift()
    {
        var harness = new WorkforceHarness();
        var leaveType = harness.SeedLeaveType("annual", "Annual", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, _) = await harness.SeedEmploymentAsync(Day.AddDays(-5));
        var shift = await CreateDayShiftAsync(harness);
        await UpsertShiftAsync(harness, employeeId, Day, shift.Id);
        await RecordLeaveAsync(harness, employeeId, leaveType.Id, Day, Day, 1.0m);

        var cell = DayOf(await QuerySeptemberAsync(harness), employeeId, Day);
        Assert.Equal(nameof(AttendanceAcceptedKind.Leave), cell.AcceptedKind);
        Assert.Equal(nameof(AttendanceSource.Leave), cell.Source);
        Assert.False(cell.IsProvisional);
        Assert.Equal("Shift", cell.Schedule!.State);
        Assert.NotNull(cell.Leave);
        Assert.Equal(leaveType.Code, cell.Leave!.LeaveTypeCode);
        Assert.Equal(LeaveTypeSystemKind.Annual, cell.Leave.SystemKind);
        Assert.Equal(1.0m, cell.Leave.Amount);
    }

    [Fact]
    public async Task CustomLeaveType_KeepsConfiguredNameAndHasNoSystemKind()
    {
        var harness = new WorkforceHarness();
        var leaveType = harness.SeedLeaveType("birthday", "Doğum Günü İzni", tracksBalance: false);
        var (employeeId, _) = await harness.SeedEmploymentAsync(Day.AddDays(-5));
        await RecordLeaveAsync(harness, employeeId, leaveType.Id, Day, Day, 1.0m);

        var cell = DayOf(await QuerySeptemberAsync(harness), employeeId, Day);
        Assert.Equal(nameof(AttendanceAcceptedKind.Leave), cell.AcceptedKind);
        Assert.Equal("birthday", cell.Leave!.LeaveTypeCode);
        Assert.Equal("Doğum Günü İzni", cell.Leave.LeaveTypeName);
        Assert.Null(cell.Leave.SystemKind);
    }

    [Fact]
    public async Task RecordedLeave_OverridesRestDay()
    {
        var harness = new WorkforceHarness();
        var leaveType = harness.SeedLeaveType("annual", "Annual", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, _) = await harness.SeedEmploymentAsync(Day.AddDays(-5));
        await UpsertRestAsync(harness, employeeId, Day);
        await RecordLeaveAsync(harness, employeeId, leaveType.Id, Day, Day, 1.0m);

        var cell = DayOf(await QuerySeptemberAsync(harness), employeeId, Day);
        Assert.Equal(nameof(AttendanceAcceptedKind.Leave), cell.AcceptedKind);
        Assert.Equal(nameof(AttendanceSource.Leave), cell.Source);
        Assert.Equal("RestDay", cell.Schedule!.State);
    }

    [Fact]
    public async Task PendingLeaveRequest_IsIgnored()
    {
        var harness = new WorkforceHarness();
        var leaveType = harness.SeedLeaveType("annual", "Annual", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, employmentId) = await harness.SeedEmploymentAsync(Day.AddDays(-5));
        var created = await harness.CreateLeaveRequest.ExecuteAsync(
            new CreateLeaveRequestCommand(
                employeeId, employmentId, leaveType.Id, Day, Day, 1.0m, "trip", "actor"),
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error?.Detail);
        Assert.Equal(LeaveRequestStatus.Pending, created.Value!.Status);

        var cell = DayOf(await QuerySeptemberAsync(harness), employeeId, Day);
        Assert.Equal(nameof(AttendanceAcceptedKind.Unresolved), cell.AcceptedKind);
        Assert.Null(cell.Leave);
    }

    [Fact]
    public async Task CancelledLeaveRecord_IsIgnored()
    {
        var harness = new WorkforceHarness();
        var leaveType = harness.SeedLeaveType("annual", "Annual", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, _) = await harness.SeedEmploymentAsync(Day.AddDays(-5));
        await RecordLeaveAsync(harness, employeeId, leaveType.Id, Day, Day, 1.0m);
        var recordId = harness.Store.LeaveRecords.Single().Id;
        var cancelled = await harness.CancelLeaveRecord.ExecuteAsync(
            new CancelLeaveRecordCommand(employeeId, recordId, "mistake", "manager"),
            CancellationToken.None);
        Assert.True(cancelled.IsSuccess, cancelled.Error?.Detail);

        var cell = DayOf(await QuerySeptemberAsync(harness), employeeId, Day);
        Assert.Equal(nameof(AttendanceAcceptedKind.Unresolved), cell.AcceptedKind);
        Assert.Null(cell.Leave);
        Assert.Single(harness.Store.LeaveRecords);
        Assert.Equal(LeaveRecordStatus.Cancelled, harness.Store.LeaveRecords[0].Status);
    }

    [Fact]
    public async Task LeaveCalendarCoverage_IgnoresAmountMismatch()
    {
        var harness = new WorkforceHarness();
        var leaveType = harness.SeedLeaveType("annual", "Annual", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, _) = await harness.SeedEmploymentAsync(new DateOnly(2026, 9, 1));
        var start = new DateOnly(2026, 9, 7);
        var end = new DateOnly(2026, 9, 11);
        await RecordLeaveAsync(harness, employeeId, leaveType.Id, start, end, amount: 1.5m);

        var record = Assert.Single(harness.Store.LeaveRecords);
        Assert.Equal(1.5m, record.Amount);
        Assert.Equal(5, end.DayNumber - start.DayNumber + 1);

        var month = await QuerySeptemberAsync(harness);
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var cell = DayOf(month, employeeId, date);
            Assert.Equal(nameof(AttendanceAcceptedKind.Leave), cell.AcceptedKind);
            Assert.Equal(1.5m, cell.Leave!.Amount);
        }

        Assert.Equal(1.5m, harness.Store.LeaveRecords.Single().Amount);
    }

    [Fact]
    public async Task ManualWorked_OverridesLeave()
    {
        var harness = new WorkforceHarness();
        var leaveType = harness.SeedLeaveType("annual", "Annual", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, employmentId) = await harness.SeedEmploymentAsync(Day.AddDays(-5));
        await RecordLeaveAsync(harness, employeeId, leaveType.Id, Day, Day, 1.0m);
        await SetCorrectionAsync(harness, employmentId, Day, "Worked", "came in");

        var cell = DayOf(await QuerySeptemberAsync(harness), employeeId, Day);
        Assert.Equal(nameof(AttendanceAcceptedKind.Worked), cell.AcceptedKind);
        Assert.Equal(nameof(AttendanceSource.Manual), cell.Source);
        Assert.True(cell.IsManual);
        Assert.False(cell.IsProvisional);
        Assert.Null(cell.AcceptedWorkedMinutes);
        Assert.NotNull(cell.Leave);
    }

    [Fact]
    public async Task ManualLeave_OverridesSchedule()
    {
        var harness = new WorkforceHarness();
        var (employeeId, employmentId) = await harness.SeedEmploymentAsync(Day.AddDays(-5));
        var shift = await CreateDayShiftAsync(harness);
        await UpsertShiftAsync(harness, employeeId, Day, shift.Id);
        await SetCorrectionAsync(harness, employmentId, Day, "Leave", "unpaid exception");

        var cell = DayOf(await QuerySeptemberAsync(harness), employeeId, Day);
        Assert.Equal(nameof(AttendanceAcceptedKind.Leave), cell.AcceptedKind);
        Assert.Equal(nameof(AttendanceSource.Manual), cell.Source);
        Assert.Null(cell.Leave);
        Assert.Equal("Shift", cell.Schedule!.State);
        Assert.Single(harness.Store.ScheduleEntries);
    }

    [Fact]
    public async Task ManualRestDay_OverridesSchedule()
    {
        var harness = new WorkforceHarness();
        var (employeeId, employmentId) = await harness.SeedEmploymentAsync(Day.AddDays(-5));
        var shift = await CreateDayShiftAsync(harness);
        await UpsertShiftAsync(harness, employeeId, Day, shift.Id);
        await SetCorrectionAsync(harness, employmentId, Day, "RestDay", "swapped rest");

        var cell = DayOf(await QuerySeptemberAsync(harness), employeeId, Day);
        Assert.Equal(nameof(AttendanceAcceptedKind.RestDay), cell.AcceptedKind);
        Assert.Equal(nameof(AttendanceSource.Manual), cell.Source);
    }

    [Fact]
    public async Task ManualAbsent_OverridesAllDerivedSources()
    {
        var harness = new WorkforceHarness();
        var leaveType = harness.SeedLeaveType("annual", "Annual", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, employmentId) = await harness.SeedEmploymentAsync(Day.AddDays(-5));
        var shift = await CreateDayShiftAsync(harness);
        await UpsertShiftAsync(harness, employeeId, Day, shift.Id);
        await RecordLeaveAsync(harness, employeeId, leaveType.Id, Day, Day, 1.0m);
        await SetCorrectionAsync(harness, employmentId, Day, "Absent", "no show");

        var cell = DayOf(await QuerySeptemberAsync(harness), employeeId, Day);
        Assert.Equal(nameof(AttendanceAcceptedKind.Absent), cell.AcceptedKind);
        Assert.Equal(nameof(AttendanceSource.Manual), cell.Source);
        Assert.Equal("no show", cell.CorrectionReason);
    }

    [Fact]
    public async Task ClearManualCorrection_FallsBackToDerivedSchedule()
    {
        var harness = new WorkforceHarness();
        var (employeeId, employmentId) = await harness.SeedEmploymentAsync(Day.AddDays(-5));
        var shift = await CreateDayShiftAsync(harness);
        await UpsertShiftAsync(harness, employeeId, Day, shift.Id);
        await SetCorrectionAsync(harness, employmentId, Day, "Absent", "no show");
        var cleared = await harness.ClearAttendanceCorrection.ExecuteAsync(
            new ClearAttendanceCorrectionCommand(employmentId, Day, "actor", harness.PropertyId),
            CancellationToken.None);
        Assert.True(cleared.IsSuccess, cleared.Error?.Detail);

        var cell = DayOf(await QuerySeptemberAsync(harness), employeeId, Day);
        Assert.Equal(nameof(AttendanceAcceptedKind.Worked), cell.AcceptedKind);
        Assert.Equal(nameof(AttendanceSource.Schedule), cell.Source);
        Assert.True(cell.IsProvisional);
        Assert.Empty(harness.Store.AttendanceCorrections);
        Assert.NotEmpty(harness.Store.AttendanceCorrectionChanges);
        Assert.Single(harness.Store.ScheduleEntries);
    }

    [Fact]
    public async Task OutsideEmployment_IsNotEmployed_NotUnresolved()
    {
        var harness = new WorkforceHarness();
        var start = new DateOnly(2026, 9, 20);
        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(startDate: start), CancellationToken.None);
        Assert.True(hired.IsSuccess, hired.Error?.Detail);

        var month = await QuerySeptemberAsync(harness);
        var row = Assert.Single(month.Employees, item => item.EmployeeId == hired.Value!.EmployeeId);
        var before = row.Days.Single(item => item.LocalDate == new DateOnly(2026, 9, 10));
        Assert.Equal(nameof(AttendanceCoverage.NotEmployed), before.Coverage);
        Assert.False(before.IsUnresolved);
        Assert.Null(before.AcceptedKind);
        var onStart = row.Days.Single(item => item.LocalDate == start);
        Assert.Equal(nameof(AttendanceCoverage.InEmployment), onStart.Coverage);
        Assert.Equal(nameof(AttendanceAcceptedKind.Unresolved), onStart.AcceptedKind);
        Assert.Equal(11, row.Totals.UnresolvedDays);
        Assert.Equal(0, row.Totals.AbsentDays);
        Assert.Equal(0, row.Totals.WorkedDays);
        Assert.DoesNotContain(row.Days, item =>
            item.Coverage == nameof(AttendanceCoverage.NotEmployed) && item.IsUnresolved);
    }

    private static async Task<AttendanceMonthDto> QuerySeptemberAsync(WorkforceHarness harness)
    {
        var result = await harness.GetAttendanceMonth.ExecuteAsync(
            2026,
            9,
            departmentId: null,
            search: null,
            scopedPropertyId: harness.PropertyId,
            allowedDepartmentIds: null,
            CancellationToken.None);
        Assert.True(result.IsSuccess, result.Error?.Detail);
        return result.Value!;
    }

    private static AttendanceDayResult DayOf(AttendanceMonthDto month, Guid employeeId, DateOnly date)
    {
        var row = Assert.Single(month.Employees, item => item.EmployeeId == employeeId);
        return row.Days.Single(item => item.LocalDate == date);
    }

    private static async Task<ShiftDefinitionDto> CreateDayShiftAsync(WorkforceHarness harness)
    {
        var created = await harness.ShiftDefinitionAdmin.CreateAsync(
            new CreateShiftDefinitionCommand("DAY", "Day", Eight, Sixteen, false, 30, "actor"),
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error?.Detail);
        return created.Value!;
    }

    private static async Task UpsertShiftAsync(
        WorkforceHarness harness,
        Guid employeeId,
        DateOnly date,
        Guid shiftDefinitionId)
    {
        var result = await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                employeeId,
                date,
                ScheduleEntryKind.Shift,
                shiftDefinitionId,
                null,
                "actor",
                harness.PropertyId),
            CancellationToken.None);
        Assert.True(result.IsSuccess, result.Error?.Detail);
    }

    private static async Task UpsertRestAsync(WorkforceHarness harness, Guid employeeId, DateOnly date)
    {
        var result = await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                employeeId,
                date,
                ScheduleEntryKind.RestDay,
                null,
                null,
                "actor",
                harness.PropertyId),
            CancellationToken.None);
        Assert.True(result.IsSuccess, result.Error?.Detail);
    }

    private static async Task RecordLeaveAsync(
        WorkforceHarness harness,
        Guid employeeId,
        Guid leaveTypeId,
        DateOnly start,
        DateOnly end,
        decimal amount)
    {
        var result = await harness.RecordLeave.ExecuteAsync(
            new RecordLeaveCommand(employeeId, null, leaveTypeId, start, end, amount, null, "actor"),
            CancellationToken.None);
        Assert.True(result.IsSuccess, result.Error?.Detail);
    }

    private static async Task SetCorrectionAsync(
        WorkforceHarness harness,
        Guid employmentId,
        DateOnly date,
        string kind,
        string reason)
    {
        var result = await harness.SetAttendanceCorrection.ExecuteAsync(
            new SetAttendanceCorrectionCommand(
                employmentId,
                date,
                kind,
                reason,
                "actor",
                harness.PropertyId),
            CancellationToken.None);
        Assert.True(result.IsSuccess, result.Error?.Detail);
    }
}
