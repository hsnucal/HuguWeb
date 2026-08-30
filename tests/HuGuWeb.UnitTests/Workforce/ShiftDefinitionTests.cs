using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class ShiftDefinitionTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 21, 8, 0, 0, TimeSpan.Zero);
    private static readonly TimeOnly Eight = new(8, 0);
    private static readonly TimeOnly Sixteen = new(16, 0);
    private static readonly TimeOnly Midnight = new(0, 0);
    private static readonly TimeOnly TwentyThree = new(23, 0);
    private static readonly TimeOnly Seven = new(7, 0);

    [Fact]
    public void TryCreate_ValidDayShift_Succeeds()
    {
        var created = ShiftDefinition.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "DAY",
            "Day Shift",
            Eight,
            Sixteen,
            endsNextDay: false,
            breakMinutes: 30,
            "actor",
            Now,
            out var definition,
            out _,
            out _);

        Assert.True(created);
        Assert.Equal("day", definition!.Code);
        Assert.Equal("Day Shift", definition.Name);
        Assert.Equal(Eight, definition.StartLocalTime);
        Assert.Equal(Sixteen, definition.EndLocalTime);
        Assert.False(definition.EndsNextDay);
        Assert.Equal(30, definition.BreakMinutes);
        Assert.True(definition.IsActive);
    }

    [Fact]
    public void TryCreate_OvernightToMidnight_Succeeds()
    {
        var created = ShiftDefinition.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "eve",
            "Evening",
            Sixteen,
            Midnight,
            endsNextDay: true,
            breakMinutes: 0,
            "actor",
            Now,
            out var definition,
            out _,
            out _);

        Assert.True(created);
        Assert.True(definition!.EndsNextDay);
        Assert.Equal(Sixteen, definition.StartLocalTime);
        Assert.Equal(Midnight, definition.EndLocalTime);
    }

    [Fact]
    public void ShiftDefinitionDto_MidnightEnd_SerializesAs00_00_Not23_59()
    {
        Assert.True(ShiftDefinition.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "eve",
            "Evening",
            Sixteen,
            Midnight,
            endsNextDay: true,
            breakMinutes: 0,
            "actor",
            Now,
            out var definition,
            out _,
            out _));

        var dto = ShiftDefinitionDto.From(definition!, semanticFieldsLocked: false);
        Assert.Equal(480, dto.GrossMinutes);
        Assert.Equal(480, dto.PlannedNetMinutes);
        Assert.Equal(Midnight, dto.EndLocalTime);

        var json = System.Text.Json.JsonSerializer.Serialize(
            dto,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            });

        Assert.Contains("00:00:00", json, StringComparison.Ordinal);
        Assert.DoesNotContain("23:59", json, StringComparison.Ordinal);
        Assert.Contains("\"endLocalTime\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void TryCreate_NightShift_Succeeds()
    {
        var created = ShiftDefinition.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "NIGHT",
            "Night",
            TwentyThree,
            Seven,
            endsNextDay: true,
            breakMinutes: 30,
            "actor",
            Now,
            out var definition,
            out _,
            out _);

        Assert.True(created);
        Assert.True(definition!.EndsNextDay);
        Assert.Equal(TwentyThree, definition.StartLocalTime);
        Assert.Equal(Seven, definition.EndLocalTime);
    }

    [Fact]
    public void TryCreate_StartEqualsEnd_IsRejected()
    {
        var created = ShiftDefinition.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "bad",
            "Bad",
            Eight,
            Eight,
            endsNextDay: false,
            breakMinutes: 0,
            "actor",
            Now,
            out _,
            out var field,
            out var errorCode);

        Assert.False(created);
        Assert.Equal(ScheduleValidation.Fields.StartLocalTime, field);
        Assert.Equal(ScheduleValidation.Codes.ShiftDefinitionInvalidTime, errorCode);
    }

    [Fact]
    public void TryCreate_NonNextDay_EndBeforeOrEqualStart_IsRejected()
    {
        var created = ShiftDefinition.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "bad",
            "Bad",
            Sixteen,
            Eight,
            endsNextDay: false,
            breakMinutes: 0,
            "actor",
            Now,
            out _,
            out var field,
            out var errorCode);

        Assert.False(created);
        Assert.Equal(ScheduleValidation.Fields.EndsNextDay, field);
        Assert.Equal(ScheduleValidation.Codes.ShiftDefinitionInvalidTime, errorCode);
    }

    [Fact]
    public void TryCreate_NegativeBreak_IsRejected()
    {
        var created = ShiftDefinition.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "bad",
            "Bad",
            Eight,
            Sixteen,
            endsNextDay: false,
            breakMinutes: -1,
            "actor",
            Now,
            out _,
            out var field,
            out var errorCode);

        Assert.False(created);
        Assert.Equal(ScheduleValidation.Fields.BreakMinutes, field);
        Assert.Equal(ScheduleValidation.Codes.ShiftDefinitionInvalidBreak, errorCode);
    }

    [Fact]
    public void TryCreate_BreakMinutesGreaterOrEqualGross_IsRejected()
    {
        var created = ShiftDefinition.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "bad",
            "Bad",
            Eight,
            Sixteen,
            endsNextDay: false,
            breakMinutes: 480,
            "actor",
            Now,
            out _,
            out var field,
            out var errorCode);

        Assert.False(created);
        Assert.Equal(ScheduleValidation.Fields.BreakMinutes, field);
        Assert.Equal(ScheduleValidation.Codes.ShiftDefinitionInvalidBreak, errorCode);
    }

    [Fact]
    public void DerivedMinutes_DayAndOvernight_AreCorrect()
    {
        Assert.True(ShiftDefinition.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "day",
            "Day",
            Eight,
            Sixteen,
            endsNextDay: false,
            breakMinutes: 30,
            "actor",
            Now,
            out var day,
            out _,
            out _));
        Assert.Equal(480, day!.GrossMinutes);
        Assert.Equal(450, day.PlannedNetMinutes);

        Assert.True(ShiftDefinition.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "night",
            "Night",
            TwentyThree,
            Seven,
            endsNextDay: true,
            breakMinutes: 30,
            "actor",
            Now,
            out var night,
            out _,
            out _));
        Assert.Equal(480, night!.GrossMinutes);
        Assert.Equal(450, night.PlannedNetMinutes);

        Assert.True(ShiftDefinition.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "eve",
            "Eve",
            Sixteen,
            Midnight,
            endsNextDay: true,
            breakMinutes: 0,
            "actor",
            Now,
            out var eve,
            out _,
            out _));
        Assert.Equal(480, eve!.GrossMinutes);
        Assert.Equal(480, eve.PlannedNetMinutes);
    }

    [Fact]
    public async Task Admin_DuplicateCodeOnSameProperty_IsRejected()
    {
        var harness = new WorkforceHarness();
        var first = await harness.ShiftDefinitionAdmin.CreateAsync(
            new CreateShiftDefinitionCommand("DAY", "Day", Eight, Sixteen, false, 30, "actor"),
            CancellationToken.None);
        Assert.True(first.IsSuccess, first.Error?.Detail);

        var duplicate = await harness.ShiftDefinitionAdmin.CreateAsync(
            new CreateShiftDefinitionCommand("day", "Day Again", Eight, Sixteen, false, 30, "actor"),
            CancellationToken.None);

        Assert.False(duplicate.IsSuccess);
        Assert.Equal(ScheduleValidation.Codes.ShiftDefinitionCodeExists, duplicate.Error!.Code);
    }

    [Fact]
    public async Task Admin_SameCodeOnOtherProperty_IsAllowed()
    {
        var harness = new WorkforceHarness();
        var first = await harness.ShiftDefinitionAdmin.CreateAsync(
            new CreateShiftDefinitionCommand("DAY", "Day A", Eight, Sixteen, false, 30, "actor"),
            CancellationToken.None);
        Assert.True(first.IsSuccess, first.Error?.Detail);

        var otherAdmin = new ShiftDefinitionAdminUseCase(
            harness.Store,
            harness.Clock,
            new FixedWorkplace(harness.OrganizationId, harness.OtherPropertyId));
        var other = await otherAdmin.CreateAsync(
            new CreateShiftDefinitionCommand("DAY", "Day B", Eight, Sixteen, false, 30, "actor"),
            CancellationToken.None);

        Assert.True(other.IsSuccess, other.Error?.Detail);
        Assert.Equal("day", other.Value!.Code);
        Assert.Equal(harness.OtherPropertyId, other.Value.PropertyId);
        Assert.Equal(2, harness.Store.ShiftDefinitions.Count);
    }

    [Fact]
    public async Task Admin_InactiveDefinition_BlocksNewSchedule_ButHistoricalStateRemainsReadable()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();
        var created = await harness.ShiftDefinitionAdmin.CreateAsync(
            new CreateShiftDefinitionCommand("DAY", "Day", Eight, Sixteen, false, 30, "actor"),
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error?.Detail);
        var scheduleDate = harness.Clock.Today;

        var scheduled = await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                employeeId,
                scheduleDate,
                ScheduleEntryKind.Shift,
                created.Value!.Id,
                Note: null,
                "actor",
                ScopedPropertyId: null),
            CancellationToken.None);
        Assert.True(scheduled.IsSuccess, scheduled.Error?.Detail);

        var deactivated = await harness.ShiftDefinitionAdmin.UpdateAsync(
            new UpdateShiftDefinitionCommand(
                created.Value.Id,
                Name: null,
                StartLocalTime: null,
                EndLocalTime: null,
                EndsNextDay: null,
                BreakMinutes: null,
                IsActive: false,
                "actor"),
            CancellationToken.None);
        Assert.True(deactivated.IsSuccess, deactivated.Error?.Detail);
        Assert.False(deactivated.Value!.IsActive);

        var rejected = await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                employeeId,
                scheduleDate.AddDays(1),
                ScheduleEntryKind.Shift,
                created.Value.Id,
                Note: null,
                "actor",
                ScopedPropertyId: null),
            CancellationToken.None);
        Assert.False(rejected.IsSuccess);
        Assert.Equal(ScheduleValidation.Codes.ShiftDefinitionInactive, rejected.Error!.Code);

        var state = await harness.GetScheduleState.ExecuteAsync(
            employeeId,
            scheduleDate,
            scopedPropertyId: null,
            CancellationToken.None);
        Assert.True(state.IsSuccess, state.Error?.Detail);
        var scheduledState = Assert.IsType<ScheduledScheduleStateDto>(state.Value);
        Assert.Equal("Scheduled", scheduledState.State);
        Assert.Equal(created.Value.Id, scheduledState.ShiftDefinitionId);
        Assert.False(scheduledState.ShiftIsActive);
    }

    [Fact]
    public async Task Admin_CodeIsImmutable_OnlyCreateSetsCode()
    {
        var harness = new WorkforceHarness();
        var created = await harness.ShiftDefinitionAdmin.CreateAsync(
            new CreateShiftDefinitionCommand("DAY", "Day", Eight, Sixteen, false, 30, "actor"),
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error?.Detail);

        var updated = await harness.ShiftDefinitionAdmin.UpdateAsync(
            new UpdateShiftDefinitionCommand(
                created.Value!.Id,
                Name: "Day Renamed",
                StartLocalTime: null,
                EndLocalTime: null,
                EndsNextDay: null,
                BreakMinutes: null,
                IsActive: null,
                "actor"),
            CancellationToken.None);

        Assert.True(updated.IsSuccess, updated.Error?.Detail);
        Assert.Equal("day", updated.Value!.Code);
        Assert.Equal("Day Renamed", updated.Value.Name);
        Assert.Null(typeof(UpdateShiftDefinitionCommand).GetProperty("Code"));
    }

    [Fact]
    public async Task Admin_SemanticTimesLocked_AfterFirstScheduleEntryUse()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();
        var created = await harness.ShiftDefinitionAdmin.CreateAsync(
            new CreateShiftDefinitionCommand("DAY", "Day", Eight, Sixteen, false, 30, "actor"),
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error?.Detail);

        var scheduled = await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                employeeId,
                harness.Clock.Today,
                ScheduleEntryKind.Shift,
                created.Value!.Id,
                Note: null,
                "actor",
                ScopedPropertyId: null),
            CancellationToken.None);
        Assert.True(scheduled.IsSuccess, scheduled.Error?.Detail);

        var locked = await harness.ShiftDefinitionAdmin.UpdateAsync(
            new UpdateShiftDefinitionCommand(
                created.Value.Id,
                Name: null,
                StartLocalTime: new TimeOnly(9, 0),
                EndLocalTime: Sixteen,
                EndsNextDay: false,
                BreakMinutes: 30,
                IsActive: null,
                "actor"),
            CancellationToken.None);

        Assert.False(locked.IsSuccess);
        Assert.Equal(ScheduleValidation.Codes.ShiftDefinitionSemanticFieldsLocked, locked.Error!.Code);
        Assert.Equal(Eight, harness.Store.ShiftDefinitions.Single().StartLocalTime);
    }

    [Fact]
    public async Task Admin_NameRemainsEditable_AfterUse()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();
        var created = await harness.ShiftDefinitionAdmin.CreateAsync(
            new CreateShiftDefinitionCommand("DAY", "Day", Eight, Sixteen, false, 30, "actor"),
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error?.Detail);
        await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                employeeId,
                harness.Clock.Today,
                ScheduleEntryKind.Shift,
                created.Value!.Id,
                Note: null,
                "actor",
                ScopedPropertyId: null),
            CancellationToken.None);

        var renamed = await harness.ShiftDefinitionAdmin.UpdateAsync(
            new UpdateShiftDefinitionCommand(
                created.Value.Id,
                Name: "Day After Use",
                StartLocalTime: null,
                EndLocalTime: null,
                EndsNextDay: null,
                BreakMinutes: null,
                IsActive: null,
                "actor"),
            CancellationToken.None);

        Assert.True(renamed.IsSuccess, renamed.Error?.Detail);
        Assert.Equal("Day After Use", renamed.Value!.Name);
        Assert.True(renamed.Value.SemanticFieldsLocked);
    }

    [Fact]
    public async Task Admin_DeactivateRemainsAllowed_AfterUse()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync();
        var created = await harness.ShiftDefinitionAdmin.CreateAsync(
            new CreateShiftDefinitionCommand("DAY", "Day", Eight, Sixteen, false, 30, "actor"),
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error?.Detail);
        await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                employeeId,
                harness.Clock.Today,
                ScheduleEntryKind.Shift,
                created.Value!.Id,
                Note: null,
                "actor",
                ScopedPropertyId: null),
            CancellationToken.None);

        var deactivated = await harness.ShiftDefinitionAdmin.UpdateAsync(
            new UpdateShiftDefinitionCommand(
                created.Value.Id,
                Name: null,
                StartLocalTime: null,
                EndLocalTime: null,
                EndsNextDay: null,
                BreakMinutes: null,
                IsActive: false,
                "actor"),
            CancellationToken.None);

        Assert.True(deactivated.IsSuccess, deactivated.Error?.Detail);
        Assert.False(deactivated.Value!.IsActive);
    }
}
