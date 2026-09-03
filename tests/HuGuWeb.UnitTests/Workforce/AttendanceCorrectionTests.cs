using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class AttendanceCorrectionTests
{
    private static readonly DateOnly Day = new(2026, 9, 10);
    private static readonly TimeOnly Eight = new(8, 0);
    private static readonly TimeOnly Sixteen = new(16, 0);

    [Fact]
    public async Task ReasonRequired()
    {
        var harness = new WorkforceHarness();
        var (_, employmentId) = await harness.SeedEmploymentAsync(Day.AddDays(-5));
        var result = await harness.SetAttendanceCorrection.ExecuteAsync(
            new SetAttendanceCorrectionCommand(
                employmentId, Day, "Absent", "  ", "actor", harness.PropertyId),
            CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(AttendanceValidation.Codes.AttendanceCorrectionReasonRequired, result.Error!.Code);
        Assert.Empty(harness.Store.AttendanceCorrections);
        Assert.Empty(harness.Store.AttendanceCorrectionChanges);
    }

    [Theory]
    [InlineData("Unresolved")]
    [InlineData("Partial")]
    [InlineData("Holiday")]
    [InlineData("1")]
    [InlineData("Punch")]
    public async Task InvalidKindRejected(string kind)
    {
        var harness = new WorkforceHarness();
        var (_, employmentId) = await harness.SeedEmploymentAsync(Day.AddDays(-5));
        var result = await harness.SetAttendanceCorrection.ExecuteAsync(
            new SetAttendanceCorrectionCommand(
                employmentId, Day, kind, "note", "actor", harness.PropertyId),
            CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(AttendanceValidation.Codes.AttendanceCorrectionKindInvalid, result.Error!.Code);
    }

    [Fact]
    public async Task DateBeforeEmploymentRejected()
    {
        var harness = new WorkforceHarness();
        var start = new DateOnly(2026, 9, 15);
        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(startDate: start), CancellationToken.None);
        Assert.True(hired.IsSuccess, hired.Error?.Detail);
        var result = await harness.SetAttendanceCorrection.ExecuteAsync(
            new SetAttendanceCorrectionCommand(
                hired.Value!.EmploymentId,
                start.AddDays(-1),
                "Absent",
                "too early",
                "actor",
                harness.PropertyId),
            CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(AttendanceValidation.Codes.AttendanceOutsideEmployment, result.Error!.Code);
    }

    [Fact]
    public async Task DateAfterEndedEmploymentRejected()
    {
        var harness = new WorkforceHarness();
        var start = new DateOnly(2026, 8, 1);
        var end = new DateOnly(2026, 9, 15);
        var (employeeId, employmentId) = await harness.SeedEmploymentAsync(start);
        var ended = await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(employeeId, end, EmploymentTerminationReason.Resignation),
            CancellationToken.None);
        Assert.True(ended.IsSuccess, ended.Error?.Detail);

        var result = await harness.SetAttendanceCorrection.ExecuteAsync(
            new SetAttendanceCorrectionCommand(
                employmentId,
                end.AddDays(1),
                "Absent",
                "too late",
                "actor",
                harness.PropertyId),
            CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(AttendanceValidation.Codes.AttendanceOutsideEmployment, result.Error!.Code);
    }

    [Fact]
    public async Task CreateAndChangeWriteHistoryWithActorAndUtc()
    {
        var harness = new WorkforceHarness();
        var firstStamp = new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.Zero);
        harness.Clock.UtcNow = firstStamp;
        var (_, employmentId) = await harness.SeedEmploymentAsync(Day.AddDays(-5));

        var created = await harness.SetAttendanceCorrection.ExecuteAsync(
            new SetAttendanceCorrectionCommand(
                employmentId, Day, "Absent", "no show", "actor-1", harness.PropertyId),
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error?.Detail);
        Assert.Equal(nameof(AttendanceAcceptedKind.Absent), created.Value!.AcceptedKind);

        var secondStamp = firstStamp.AddHours(2);
        harness.Clock.UtcNow = secondStamp;
        var changed = await harness.SetAttendanceCorrection.ExecuteAsync(
            new SetAttendanceCorrectionCommand(
                employmentId, Day, "Worked", "arrived later", "actor-2", harness.PropertyId),
            CancellationToken.None);
        Assert.True(changed.IsSuccess, changed.Error?.Detail);
        Assert.Equal(nameof(AttendanceAcceptedKind.Worked), changed.Value!.AcceptedKind);
        Assert.False(changed.Value.IsProvisional);
        Assert.Null(changed.Value.AcceptedWorkedMinutes);

        Assert.Single(harness.Store.AttendanceCorrections);
        Assert.Equal(2, harness.Store.AttendanceCorrectionChanges.Count);

        var history = await harness.GetAttendanceHistory.ExecuteAsync(
            employmentId, Day, harness.PropertyId, null, CancellationToken.None);
        Assert.True(history.IsSuccess, history.Error?.Detail);
        Assert.Equal(2, history.Value!.Changes.Count);
        Assert.Equal("Set", history.Value.Changes[0].ChangeType);
        Assert.Null(history.Value.Changes[0].PreviousKind);
        Assert.Equal("Absent", history.Value.Changes[0].NewKind);
        Assert.Equal("no show", history.Value.Changes[0].NewReason);
        Assert.Equal("actor-1", history.Value.Changes[0].ChangedByUserId);
        Assert.Equal(firstStamp, history.Value.Changes[0].ChangedAtUtc);
        Assert.Equal("Absent", history.Value.Changes[1].PreviousKind);
        Assert.Equal("Worked", history.Value.Changes[1].NewKind);
        Assert.Equal("arrived later", history.Value.Changes[1].NewReason);
        Assert.Equal("actor-2", history.Value.Changes[1].ChangedByUserId);
        Assert.Equal(secondStamp, history.Value.Changes[1].ChangedAtUtc);
        Assert.Equal(TimeSpan.Zero, history.Value.Changes[0].ChangedAtUtc.Offset);
    }

    [Fact]
    public async Task ClearWritesHistoryAndPreservesAudit()
    {
        var harness = new WorkforceHarness();
        var setStamp = new DateTimeOffset(2026, 9, 10, 8, 0, 0, TimeSpan.Zero);
        harness.Clock.UtcNow = setStamp;
        var (_, employmentId) = await harness.SeedEmploymentAsync(Day.AddDays(-5));
        await harness.SetAttendanceCorrection.ExecuteAsync(
            new SetAttendanceCorrectionCommand(
                employmentId, Day, "Absent", "no show", "actor", harness.PropertyId),
            CancellationToken.None);

        harness.Clock.UtcNow = setStamp.AddMinutes(5);
        var cleared = await harness.ClearAttendanceCorrection.ExecuteAsync(
            new ClearAttendanceCorrectionCommand(employmentId, Day, "clear-actor", harness.PropertyId),
            CancellationToken.None);
        Assert.True(cleared.IsSuccess, cleared.Error?.Detail);
        Assert.Empty(harness.Store.AttendanceCorrections);

        var history = await harness.GetAttendanceHistory.ExecuteAsync(
            employmentId, Day, harness.PropertyId, null, CancellationToken.None);
        Assert.True(history.IsSuccess, history.Error?.Detail);
        Assert.Equal(2, history.Value!.Changes.Count);
        Assert.Equal("Clear", history.Value.Changes[1].ChangeType);
        Assert.Equal("Absent", history.Value.Changes[1].PreviousKind);
        Assert.Null(history.Value.Changes[1].NewKind);
        Assert.Equal("clear-actor", history.Value.Changes[1].ChangedByUserId);
    }

    [Fact]
    public async Task PastMonthCorrection_IsAllowed()
    {
        var harness = new WorkforceHarness();
        harness.Clock.Today = new DateOnly(2026, 9, 2);
        var past = new DateOnly(2026, 8, 15);
        var (_, employmentId) = await harness.SeedEmploymentAsync(new DateOnly(2026, 8, 1));
        var result = await harness.SetAttendanceCorrection.ExecuteAsync(
            new SetAttendanceCorrectionCommand(
                employmentId, past, "Absent", "late catch-up", "actor", harness.PropertyId),
            CancellationToken.None);
        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Equal(nameof(AttendanceAcceptedKind.Absent), result.Value!.AcceptedKind);
    }

    [Fact]
    public async Task UnchangedCorrection_DoesNotWriteExtraHistory()
    {
        var harness = new WorkforceHarness();
        var (_, employmentId) = await harness.SeedEmploymentAsync(Day.AddDays(-5));
        var first = await harness.SetAttendanceCorrection.ExecuteAsync(
            new SetAttendanceCorrectionCommand(
                employmentId, Day, "Absent", "no show", "actor", harness.PropertyId),
            CancellationToken.None);
        Assert.True(first.IsSuccess, first.Error?.Detail);
        var second = await harness.SetAttendanceCorrection.ExecuteAsync(
            new SetAttendanceCorrectionCommand(
                employmentId, Day, "Absent", "no show", "actor", harness.PropertyId),
            CancellationToken.None);
        Assert.True(second.IsSuccess, second.Error?.Detail);
        Assert.Single(harness.Store.AttendanceCorrectionChanges);
    }

    [Fact]
    public async Task CorrectionDoesNotMutateScheduleEntry()
    {
        var harness = new WorkforceHarness();
        var (employeeId, employmentId) = await harness.SeedEmploymentAsync(Day.AddDays(-5));
        var created = await harness.ShiftDefinitionAdmin.CreateAsync(
            new CreateShiftDefinitionCommand("DAY", "Day", Eight, Sixteen, false, 30, "actor"),
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error?.Detail);
        var scheduled = await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                employeeId, Day, ScheduleEntryKind.Shift, created.Value!.Id, null, "actor", harness.PropertyId),
            CancellationToken.None);
        Assert.True(scheduled.IsSuccess, scheduled.Error?.Detail);
        var before = harness.Store.ScheduleEntries.Single();

        await harness.SetAttendanceCorrection.ExecuteAsync(
            new SetAttendanceCorrectionCommand(
                employmentId, Day, "Absent", "no show", "actor", harness.PropertyId),
            CancellationToken.None);

        var after = harness.Store.ScheduleEntries.Single();
        Assert.Equal(before.Id, after.Id);
        Assert.Equal(ScheduleEntryKind.Shift, after.Kind);
        Assert.Equal(before.ShiftDefinitionId, after.ShiftDefinitionId);
    }
}
