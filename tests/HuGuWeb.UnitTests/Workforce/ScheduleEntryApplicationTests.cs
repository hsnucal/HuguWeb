using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class ScheduleEntryApplicationTests
{
    private static readonly TimeOnly Eight = new(8, 0);
    private static readonly TimeOnly Sixteen = new(16, 0);
    private static readonly TimeOnly TwentyThree = new(23, 0);
    private static readonly TimeOnly Seven = new(7, 0);

    [Fact]
    public async Task Upsert_Shift_RequiresShiftDefinitionId()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();

        var result = await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                employeeId,
                harness.Clock.Today,
                ScheduleEntryKind.Shift,
                ShiftDefinitionId: null,
                Note: null,
                "actor",
                ScopedPropertyId: null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ScheduleValidation.Codes.ScheduleShiftDefinitionRequired, result.Error!.Code);
    }

    [Fact]
    public async Task Upsert_RestDay_RejectsShiftDefinitionId()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();
        var definition = await CreateDayShiftAsync(harness);

        var result = await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                employeeId,
                harness.Clock.Today,
                ScheduleEntryKind.RestDay,
                definition.Id,
                Note: null,
                "actor",
                ScopedPropertyId: null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ScheduleValidation.Codes.ScheduleShiftDefinitionMustBeNull, result.Error!.Code);
    }

    [Fact]
    public async Task Upsert_Shift_StoresCoveringPrimaryAssignmentId()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();
        var definition = await CreateDayShiftAsync(harness);
        var primary = harness.Store.Assignments.Single(item => item.EndDate is null);

        var result = await harness.UpsertSchedule.ExecuteAsync(
            ShiftCommand(employeeId, harness.Clock.Today, definition.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        var entry = Assert.Single(harness.Store.ScheduleEntries);
        Assert.Equal(ScheduleEntryKind.Shift, entry.Kind);
        Assert.Equal(primary.Id, entry.AssignmentId);
        Assert.Equal(definition.Id, entry.ShiftDefinitionId);
    }

    [Fact]
    public async Task Upsert_RestDay_CreatesEntry()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();

        var result = await harness.UpsertSchedule.ExecuteAsync(
            RestCommand(employeeId, harness.Clock.Today),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        var entry = Assert.Single(harness.Store.ScheduleEntries);
        Assert.Equal(ScheduleEntryKind.RestDay, entry.Kind);
        Assert.Null(entry.ShiftDefinitionId);
        Assert.IsType<RestDayScheduleStateDto>(result.Value);
    }

    [Fact]
    public async Task GetScheduleState_WithNoRow_IsUnscheduled()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();

        var state = await harness.GetScheduleState.ExecuteAsync(
            employeeId,
            harness.Clock.Today,
            scopedPropertyId: null,
            CancellationToken.None);

        Assert.True(state.IsSuccess, state.Error?.Detail);
        var unscheduled = Assert.IsType<UnscheduledScheduleStateDto>(state.Value);
        Assert.Equal("Unscheduled", unscheduled.State);
        Assert.Null(unscheduled.ScheduleEntryId);
    }

    [Fact]
    public async Task Upsert_ShiftToShift_WritesHistory()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();
        var day = await CreateDayShiftAsync(harness, "DAY");
        var eve = await CreateDayShiftAsync(harness, "EVE", new TimeOnly(16, 0), new TimeOnly(0, 0), endsNextDay: true);
        var date = harness.Clock.Today;

        Assert.True((await harness.UpsertSchedule.ExecuteAsync(ShiftCommand(employeeId, date, day.Id), CancellationToken.None)).IsSuccess);
        var updated = await harness.UpsertSchedule.ExecuteAsync(ShiftCommand(employeeId, date, eve.Id), CancellationToken.None);

        Assert.True(updated.IsSuccess, updated.Error?.Detail);
        Assert.Equal(2, harness.Store.ScheduleEntryChanges.Count);
        var change = harness.Store.ScheduleEntryChanges.Last();
        Assert.Equal(ScheduleEntryKind.Shift, change.PreviousKind);
        Assert.Equal(day.Id, change.PreviousShiftDefinitionId);
        Assert.Equal(ScheduleEntryKind.Shift, change.NewKind);
        Assert.Equal(eve.Id, change.NewShiftDefinitionId);
        Assert.Single(harness.Store.ScheduleEntries);
    }

    [Fact]
    public async Task Upsert_ShiftToRestDay_WritesHistory()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();
        var day = await CreateDayShiftAsync(harness);
        var date = harness.Clock.Today;

        Assert.True((await harness.UpsertSchedule.ExecuteAsync(ShiftCommand(employeeId, date, day.Id), CancellationToken.None)).IsSuccess);
        var updated = await harness.UpsertSchedule.ExecuteAsync(RestCommand(employeeId, date), CancellationToken.None);

        Assert.True(updated.IsSuccess, updated.Error?.Detail);
        var change = harness.Store.ScheduleEntryChanges.Last();
        Assert.Equal(ScheduleEntryKind.Shift, change.PreviousKind);
        Assert.Equal(day.Id, change.PreviousShiftDefinitionId);
        Assert.Equal(ScheduleEntryKind.RestDay, change.NewKind);
        Assert.Null(change.NewShiftDefinitionId);
    }

    [Fact]
    public async Task Upsert_RestDayToShift_WritesHistory()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();
        var day = await CreateDayShiftAsync(harness);
        var date = harness.Clock.Today;

        Assert.True((await harness.UpsertSchedule.ExecuteAsync(RestCommand(employeeId, date), CancellationToken.None)).IsSuccess);
        var updated = await harness.UpsertSchedule.ExecuteAsync(ShiftCommand(employeeId, date, day.Id), CancellationToken.None);

        Assert.True(updated.IsSuccess, updated.Error?.Detail);
        var change = harness.Store.ScheduleEntryChanges.Last();
        Assert.Equal(ScheduleEntryKind.RestDay, change.PreviousKind);
        Assert.Null(change.PreviousShiftDefinitionId);
        Assert.Equal(ScheduleEntryKind.Shift, change.NewKind);
        Assert.Equal(day.Id, change.NewShiftDefinitionId);
    }

    [Fact]
    public async Task Clear_Shift_ToUnscheduled_RetainsHistory()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();
        var day = await CreateDayShiftAsync(harness);
        var date = harness.Clock.Today;

        Assert.True((await harness.UpsertSchedule.ExecuteAsync(ShiftCommand(employeeId, date, day.Id), CancellationToken.None)).IsSuccess);
        var cleared = await harness.ClearSchedule.ExecuteAsync(
            new ClearScheduleEntryCommand(employeeId, date, "actor", ScopedPropertyId: null),
            CancellationToken.None);

        Assert.True(cleared.IsSuccess, cleared.Error?.Detail);
        Assert.IsType<UnscheduledScheduleStateDto>(cleared.Value);
        Assert.Empty(harness.Store.ScheduleEntries);
        var clearChange = harness.Store.ScheduleEntryChanges.Last();
        Assert.Equal(ScheduleEntryKind.Shift, clearChange.PreviousKind);
        Assert.Equal(day.Id, clearChange.PreviousShiftDefinitionId);
        Assert.Null(clearChange.NewKind);
        Assert.Null(clearChange.NewShiftDefinitionId);
    }

    [Fact]
    public async Task Clear_RestDay_ToUnscheduled_RetainsHistory()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();
        var date = harness.Clock.Today;

        Assert.True((await harness.UpsertSchedule.ExecuteAsync(RestCommand(employeeId, date), CancellationToken.None)).IsSuccess);
        var cleared = await harness.ClearSchedule.ExecuteAsync(
            new ClearScheduleEntryCommand(employeeId, date, "actor", ScopedPropertyId: null),
            CancellationToken.None);

        Assert.True(cleared.IsSuccess, cleared.Error?.Detail);
        Assert.Empty(harness.Store.ScheduleEntries);
        var clearChange = harness.Store.ScheduleEntryChanges.Last();
        Assert.Equal(ScheduleEntryKind.RestDay, clearChange.PreviousKind);
        Assert.Null(clearChange.NewKind);
    }

    [Fact]
    public async Task Clear_AlreadyUnscheduled_IsSuccessNoOp()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();

        var cleared = await harness.ClearSchedule.ExecuteAsync(
            new ClearScheduleEntryCommand(employeeId, harness.Clock.Today, "actor", ScopedPropertyId: null),
            CancellationToken.None);

        Assert.True(cleared.IsSuccess, cleared.Error?.Detail);
        Assert.IsType<UnscheduledScheduleStateDto>(cleared.Value);
        Assert.Empty(harness.Store.ScheduleEntries);
        Assert.Empty(harness.Store.ScheduleEntryChanges);
    }

    [Fact]
    public async Task Upsert_BeforeEmploymentStartDate_IsRejected()
    {
        var harness = new WorkforceHarness();
        var start = new DateOnly(2026, 7, 22);
        var (employeeId, _) = await harness.SeedEmploymentAsync(startDate: start);
        var day = await CreateDayShiftAsync(harness);

        var result = await harness.UpsertSchedule.ExecuteAsync(
            ShiftCommand(employeeId, start.AddDays(-1), day.Id),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ScheduleValidation.Codes.ScheduleEmploymentNotCoveringDate, result.Error!.Code);
    }

    [Fact]
    public async Task Upsert_AfterEmploymentEndDate_IsRejected()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync(startDate: new DateOnly(2026, 1, 1));
        var day = await CreateDayShiftAsync(harness);
        var ended = await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(employeeId, new DateOnly(2026, 6, 30), EmploymentTerminationReason.Resignation),
            CancellationToken.None);
        Assert.True(ended.IsSuccess, ended.Error?.Detail);

        var result = await harness.UpsertSchedule.ExecuteAsync(
            ShiftCommand(employeeId, new DateOnly(2026, 7, 1), day.Id),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ScheduleValidation.Codes.ScheduleEmploymentNotCoveringDate, result.Error!.Code);
    }

    [Fact]
    public async Task Upsert_OnEmploymentEndDate_IsAllowed()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync(startDate: new DateOnly(2026, 1, 1));
        var day = await CreateDayShiftAsync(harness);
        var endDate = new DateOnly(2026, 6, 30);
        Assert.True((await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(employeeId, endDate, EmploymentTerminationReason.Resignation),
            CancellationToken.None)).IsSuccess);

        var result = await harness.UpsertSchedule.ExecuteAsync(
            ShiftCommand(employeeId, endDate, day.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.IsType<ScheduledScheduleStateDto>(result.Value);
    }

    [Fact]
    public async Task Upsert_OvernightStartingOnEndDate_IsAllowed()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync(startDate: new DateOnly(2026, 1, 1));
        var night = await CreateDayShiftAsync(harness, "NIGHT", TwentyThree, Seven, endsNextDay: true, breakMinutes: 30);
        var endDate = new DateOnly(2026, 6, 30);
        Assert.True((await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(employeeId, endDate, EmploymentTerminationReason.Resignation),
            CancellationToken.None)).IsSuccess);

        var result = await harness.UpsertSchedule.ExecuteAsync(
            ShiftCommand(employeeId, endDate, night.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        var scheduled = Assert.IsType<ScheduledScheduleStateDto>(result.Value);
        Assert.True(scheduled.EndsNextDay);
        Assert.Equal(endDate, scheduled.StartLocalDate);
        Assert.Equal(endDate.AddDays(1), scheduled.EndLocalDate);
    }

    [Fact]
    public async Task Upsert_WithoutCoveringPrimaryAssignment_IsRejected()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync(startDate: new DateOnly(2026, 1, 1));
        var day = await CreateDayShiftAsync(harness);
        var primary = harness.Store.Assignments.Single();
        Assert.True(primary.TryCloseOn(new DateOnly(2026, 3, 1), out _));

        var result = await harness.UpsertSchedule.ExecuteAsync(
            ShiftCommand(employeeId, new DateOnly(2026, 3, 15), day.Id),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ScheduleValidation.Codes.ScheduleAssignmentNotFound, result.Error!.Code);
    }

    [Fact]
    public async Task Upsert_CrossPropertyShiftDefinition_IsRejected()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();
        var otherAdmin = new ShiftDefinitionAdminUseCase(
            harness.Store,
            harness.Clock,
            new FixedWorkplace(harness.OrganizationId, harness.OtherPropertyId));
        var otherDef = await otherAdmin.CreateAsync(
            new CreateShiftDefinitionCommand("DAY", "Other Day", Eight, Sixteen, false, 30, "actor"),
            CancellationToken.None);
        Assert.True(otherDef.IsSuccess, otherDef.Error?.Detail);

        var result = await harness.UpsertSchedule.ExecuteAsync(
            ShiftCommand(employeeId, harness.Clock.Today, otherDef.Value!.Id),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ScheduleValidation.Codes.ScheduleCrossPropertyShift, result.Error!.Code);
    }

    [Fact]
    public async Task Upsert_InactiveShiftDefinition_IsRejected()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();
        var created = await CreateDayShiftAsync(harness);
        Assert.True((await harness.ShiftDefinitionAdmin.UpdateAsync(
            new UpdateShiftDefinitionCommand(
                created.Id, null, null, null, null, null, IsActive: false, "actor"),
            CancellationToken.None)).IsSuccess);

        var result = await harness.UpsertSchedule.ExecuteAsync(
            ShiftCommand(employeeId, harness.Clock.Today, created.Id),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ScheduleValidation.Codes.ShiftDefinitionInactive, result.Error!.Code);
    }

    [Fact]
    public async Task Upsert_AfterCrossPropertyTransfer_UsesAssignmentPropertyForDate()
    {
        var harness = new WorkforceHarness();
        var start = harness.Clock.Today.AddDays(-20);
        var transferDate = harness.Clock.Today.AddDays(-5);
        var beforeDate = transferDate.AddDays(-2);
        var afterDate = transferDate.AddDays(2);

        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(startDate: start), CancellationToken.None);
        Assert.True(hired.IsSuccess, hired.Error?.Detail);
        var employeeId = hired.Value!.EmployeeId;
        var propertyAAssignmentId = hired.Value.AssignmentId;

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
        var propertyBAssignmentId = transferred.Value!.NewAssignmentId;

        var defA = await CreateDayShiftAsync(harness, "DAY-A");
        var otherAdmin = new ShiftDefinitionAdminUseCase(
            harness.Store,
            harness.Clock,
            new FixedWorkplace(harness.OrganizationId, harness.OtherPropertyId));
        var defB = await otherAdmin.CreateAsync(
            new CreateShiftDefinitionCommand("DAY-B", "Day B", Eight, Sixteen, false, 30, "actor"),
            CancellationToken.None);
        Assert.True(defB.IsSuccess, defB.Error?.Detail);

        var before = await harness.UpsertSchedule.ExecuteAsync(
            ShiftCommand(employeeId, beforeDate, defA.Id),
            CancellationToken.None);
        Assert.True(before.IsSuccess, before.Error?.Detail);
        Assert.Equal(propertyAAssignmentId, before.Value!.AssignmentId);
        Assert.Equal(harness.PropertyId, before.Value.PropertyId);

        var after = await harness.UpsertSchedule.ExecuteAsync(
            ShiftCommand(employeeId, afterDate, defB.Value!.Id),
            CancellationToken.None);
        Assert.True(after.IsSuccess, after.Error?.Detail);
        Assert.Equal(propertyBAssignmentId, after.Value!.AssignmentId);
        Assert.Equal(harness.OtherPropertyId, after.Value.PropertyId);
    }

    [Fact]
    public async Task GetScheduleState_Scheduled_ReturnsLocalTimesAndDurations()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();
        var day = await CreateDayShiftAsync(harness);
        var date = harness.Clock.Today;
        Assert.True((await harness.UpsertSchedule.ExecuteAsync(ShiftCommand(employeeId, date, day.Id), CancellationToken.None)).IsSuccess);

        var state = await harness.GetScheduleState.ExecuteAsync(employeeId, date, null, CancellationToken.None);

        Assert.True(state.IsSuccess, state.Error?.Detail);
        var scheduled = Assert.IsType<ScheduledScheduleStateDto>(state.Value);
        Assert.Equal("Scheduled", scheduled.State);
        Assert.Equal(date, scheduled.StartLocalDate);
        Assert.Equal(Eight, scheduled.StartLocalTime);
        Assert.Equal(date, scheduled.EndLocalDate);
        Assert.Equal(Sixteen, scheduled.EndLocalTime);
        Assert.False(scheduled.EndsNextDay);
        Assert.Equal(30, scheduled.BreakMinutes);
        Assert.Equal(480, scheduled.GrossMinutes);
        Assert.Equal(450, scheduled.PlannedNetMinutes);
        Assert.Equal(harness.PropertyId, scheduled.PropertyId);
        Assert.Equal("Test Property", scheduled.PropertyName);
        Assert.Equal(day.Id, scheduled.ShiftDefinitionId);
    }

    [Fact]
    public async Task GetScheduleState_RestDay_ReturnsRestDayDto()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();
        Assert.True((await harness.UpsertSchedule.ExecuteAsync(RestCommand(employeeId, harness.Clock.Today), CancellationToken.None)).IsSuccess);

        var state = await harness.GetScheduleState.ExecuteAsync(
            employeeId, harness.Clock.Today, null, CancellationToken.None);

        Assert.True(state.IsSuccess, state.Error?.Detail);
        var rest = Assert.IsType<RestDayScheduleStateDto>(state.Value);
        Assert.Equal("RestDay", rest.State);
        Assert.NotNull(rest.ScheduleEntryId);
    }

    [Fact]
    public async Task GetScheduleState_Unscheduled_ReturnsUnscheduledDto()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();

        var state = await harness.GetScheduleState.ExecuteAsync(
            employeeId, harness.Clock.Today, null, CancellationToken.None);

        Assert.True(state.IsSuccess, state.Error?.Detail);
        Assert.IsType<UnscheduledScheduleStateDto>(state.Value);
        Assert.Equal("Unscheduled", state.Value!.State);
    }

    [Fact]
    public async Task GetScheduleState_Scheduled_DoesNotIncludeLeaveFields()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();
        var day = await CreateDayShiftAsync(harness);
        Assert.True((await harness.UpsertSchedule.ExecuteAsync(
            ShiftCommand(employeeId, harness.Clock.Today, day.Id), CancellationToken.None)).IsSuccess);

        var state = await harness.GetScheduleState.ExecuteAsync(
            employeeId, harness.Clock.Today, null, CancellationToken.None);

        Assert.True(state.IsSuccess, state.Error?.Detail);
        var scheduled = Assert.IsType<ScheduledScheduleStateDto>(state.Value);
        Assert.Null(scheduled.GetType().GetProperty("LeaveRecordId"));
        Assert.Null(scheduled.GetType().GetProperty("LeaveTypeId"));
        Assert.Null(typeof(ScheduleStateDto).GetProperty("LeaveAmount"));
    }

    private static UpsertScheduleEntryCommand ShiftCommand(Guid employeeId, DateOnly date, Guid shiftDefinitionId) =>
        new(employeeId, date, ScheduleEntryKind.Shift, shiftDefinitionId, Note: null, "actor", ScopedPropertyId: null);

    private static UpsertScheduleEntryCommand RestCommand(Guid employeeId, DateOnly date) =>
        new(employeeId, date, ScheduleEntryKind.RestDay, ShiftDefinitionId: null, Note: null, "actor", ScopedPropertyId: null);

    private static async Task<ShiftDefinitionDto> CreateDayShiftAsync(
        WorkforceHarness harness,
        string code = "DAY",
        TimeOnly? start = null,
        TimeOnly? end = null,
        bool endsNextDay = false,
        int breakMinutes = 30)
    {
        var created = await harness.ShiftDefinitionAdmin.CreateAsync(
            new CreateShiftDefinitionCommand(
                code,
                code,
                start ?? Eight,
                end ?? Sixteen,
                endsNextDay,
                breakMinutes,
                "actor"),
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error?.Detail);
        return created.Value!;
    }
}
