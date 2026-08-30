using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class ScheduleBulkAndCopyTests
{
    private static readonly TimeOnly Eight = new(8, 0);
    private static readonly TimeOnly Sixteen = new(16, 0);
    private static readonly DateOnly WeekStart = new(2026, 8, 24);

    [Fact]
    public async Task Bulk_AssignsShiftRestDayAndClear_WritesHistory()
    {
        var harness = new WorkforceHarness();
        var day = await CreateDayShiftAsync(harness);
        var (employeeId, _) = await harness.SeedEmploymentAsync(WeekStart.AddDays(-7));

        var result = await harness.BulkSchedule.ExecuteAsync(
            new BulkScheduleCommand(
                [
                    new BulkScheduleOperation(employeeId, WeekStart, false, ScheduleEntryKind.Shift, day.Id, null),
                    new BulkScheduleOperation(employeeId, WeekStart.AddDays(1), false, ScheduleEntryKind.RestDay, null, null)
                ],
                "actor",
                null),
            CancellationToken.None);
        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Equal(2, harness.Store.ScheduleEntries.Count);
        Assert.Equal(2, harness.Store.ScheduleEntryChanges.Count);

        var clear = await harness.BulkSchedule.ExecuteAsync(
            new BulkScheduleCommand(
                [new BulkScheduleOperation(employeeId, WeekStart, Clear: true, null, null, null)],
                "actor",
                null),
            CancellationToken.None);
        Assert.True(clear.IsSuccess, clear.Error?.Detail);
        Assert.Single(harness.Store.ScheduleEntries);
        Assert.Equal(3, harness.Store.ScheduleEntryChanges.Count);
    }

    [Fact]
    public async Task Bulk_AllOrNothing_RollsBackOnCrossDepartmentFailure()
    {
        var harness = new WorkforceHarness();
        var day = await CreateDayShiftAsync(harness);
        var (hkEmployee, _) = await harness.SeedEmploymentAsync(WeekStart.AddDays(-7));
        var fo = await harness.Hire.ExecuteAsync(
            harness.HireCommand(
                startDate: WeekStart.AddDays(-7),
                departmentId: harness.OtherDepartmentId,
                positionId: harness.OtherPositionId),
            CancellationToken.None);
        Assert.True(fo.IsSuccess, fo.Error?.Detail);

        var beforeEntries = harness.Store.ScheduleEntries.Count;
        var beforeChanges = harness.Store.ScheduleEntryChanges.Count;
        var allowed = new HashSet<Guid> { harness.DepartmentId };

        var result = await harness.BulkSchedule.ExecuteAsync(
            new BulkScheduleCommand(
                [
                    new BulkScheduleOperation(hkEmployee, WeekStart, false, ScheduleEntryKind.Shift, day.Id, null),
                    new BulkScheduleOperation(fo.Value!.EmployeeId, WeekStart, false, ScheduleEntryKind.Shift, day.Id, null)
                ],
                "actor",
                harness.PropertyId,
                allowed),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ScheduleValidation.Codes.ScheduleBulkFailed, result.Error!.Code);
        Assert.Equal(beforeEntries, harness.Store.ScheduleEntries.Count);
        Assert.Equal(beforeChanges, harness.Store.ScheduleEntryChanges.Count);
    }

    [Fact]
    public async Task Bulk_InactiveDefinition_RollsBackAll()
    {
        var harness = new WorkforceHarness();
        var day = await CreateDayShiftAsync(harness);
        var (employeeId, _) = await harness.SeedEmploymentAsync(WeekStart.AddDays(-7));
        Assert.True((await harness.ShiftDefinitionAdmin.UpdateAsync(
            new UpdateShiftDefinitionCommand(day.Id, null, null, null, null, null, false, "actor"),
            CancellationToken.None)).IsSuccess);

        var result = await harness.BulkSchedule.ExecuteAsync(
            new BulkScheduleCommand(
                [
                    new BulkScheduleOperation(employeeId, WeekStart, false, ScheduleEntryKind.RestDay, null, null),
                    new BulkScheduleOperation(employeeId, WeekStart.AddDays(1), false, ScheduleEntryKind.Shift, day.Id, null)
                ],
                "actor",
                null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Empty(harness.Store.ScheduleEntries);
        Assert.Empty(harness.Store.ScheduleEntryChanges);
    }

    [Fact]
    public async Task Bulk_InvalidEmploymentDate_RollsBackAll()
    {
        var harness = new WorkforceHarness();
        var day = await CreateDayShiftAsync(harness);
        var start = WeekStart.AddDays(2);
        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(startDate: start), CancellationToken.None);
        Assert.True(hired.IsSuccess, hired.Error?.Detail);

        var result = await harness.BulkSchedule.ExecuteAsync(
            new BulkScheduleCommand(
                [
                    new BulkScheduleOperation(hired.Value!.EmployeeId, WeekStart.AddDays(3), false, ScheduleEntryKind.Shift, day.Id, null),
                    new BulkScheduleOperation(hired.Value.EmployeeId, WeekStart, false, ScheduleEntryKind.Shift, day.Id, null)
                ],
                "actor",
                null),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Empty(harness.Store.ScheduleEntries);
        Assert.Empty(harness.Store.ScheduleEntryChanges);
    }

    [Fact]
    public async Task CopyWeek_CopiesShiftAndRestDay_NotUnscheduled_ReResolvesAssignment()
    {
        var harness = new WorkforceHarness();
        var day = await CreateDayShiftAsync(harness);
        var (employeeId, _) = await harness.SeedEmploymentAsync(WeekStart.AddDays(-14));

        Assert.True((await harness.UpsertSchedule.ExecuteAsync(
            ShiftCommand(employeeId, WeekStart, day.Id), CancellationToken.None)).IsSuccess);
        Assert.True((await harness.UpsertSchedule.ExecuteAsync(
            RestCommand(employeeId, WeekStart.AddDays(1)), CancellationToken.None)).IsSuccess);

        var transferDate = WeekStart.AddDays(7); // Monday of target week
        Assert.True((await harness.Transfer.ExecuteAsync(
            new TransferEmployeeCommand(
                employeeId,
                harness.OtherDepartmentId,
                harness.OtherPositionId,
                transferDate),
            CancellationToken.None)).IsSuccess);

        var targetWeek = WeekStart.AddDays(7);
        var preview = await harness.CopyScheduleWeek.PreviewAsync(
            new CopyScheduleWeekCommand(targetWeek, null, "actor", null),
            CancellationToken.None);
        Assert.True(preview.IsSuccess, preview.Error?.Detail);
        Assert.Equal(2, preview.Value!.CopyCount);
        Assert.Equal(0, preview.Value.InvalidCount);
        Assert.All(preview.Value.Operations, op =>
        {
            Assert.Equal(harness.OtherDepartmentId, op.TargetDepartmentId);
            Assert.NotEqual(Guid.Empty, op.TargetAssignmentId);
        });

        var sourceAssignment = harness.Store.ScheduleEntries
            .Single(item => item.ScheduleDate == WeekStart)
            .AssignmentId;
        Assert.All(preview.Value.Operations, op => Assert.NotEqual(sourceAssignment, op.TargetAssignmentId));

        var applied = await harness.CopyScheduleWeek.ExecuteAsync(
            new CopyScheduleWeekCommand(targetWeek, null, "actor", null),
            CancellationToken.None);
        Assert.True(applied.IsSuccess, applied.Error?.Detail);
        Assert.Equal(4, harness.Store.ScheduleEntries.Count);
        Assert.Contains(
            harness.Store.ScheduleEntries,
            item => item.ScheduleDate == targetWeek && item.Kind == ScheduleEntryKind.Shift);
        Assert.Contains(
            harness.Store.ScheduleEntries,
            item => item.ScheduleDate == targetWeek.AddDays(1) && item.Kind == ScheduleEntryKind.RestDay);
        Assert.True(harness.Store.ScheduleEntryChanges.Count >= 4);
    }

    [Fact]
    public async Task CopyWeek_DoesNotCopyUnscheduled()
    {
        var harness = new WorkforceHarness();
        var (employeeId, _) = await harness.SeedEmploymentAsync(WeekStart.AddDays(-7));
        _ = employeeId;
        var preview = await harness.CopyScheduleWeek.PreviewAsync(
            new CopyScheduleWeekCommand(WeekStart.AddDays(7), harness.DepartmentId, "actor", null),
            CancellationToken.None);
        Assert.True(preview.IsSuccess, preview.Error?.Detail);
        Assert.Equal(0, preview.Value!.CopyCount);
    }

    [Fact]
    public async Task CopyWeek_InactiveDefinition_BlocksTarget()
    {
        var harness = new WorkforceHarness();
        var day = await CreateDayShiftAsync(harness);
        var (employeeId, _) = await harness.SeedEmploymentAsync(WeekStart.AddDays(-7));
        Assert.True((await harness.UpsertSchedule.ExecuteAsync(
            ShiftCommand(employeeId, WeekStart, day.Id), CancellationToken.None)).IsSuccess);
        Assert.True((await harness.ShiftDefinitionAdmin.UpdateAsync(
            new UpdateShiftDefinitionCommand(day.Id, null, null, null, null, null, false, "actor"),
            CancellationToken.None)).IsSuccess);

        var preview = await harness.CopyScheduleWeek.PreviewAsync(
            new CopyScheduleWeekCommand(WeekStart.AddDays(7), null, "actor", null),
            CancellationToken.None);
        Assert.True(preview.IsSuccess, preview.Error?.Detail);
        Assert.Equal(1, preview.Value!.InvalidCount);
        Assert.Equal(ScheduleValidation.Codes.ShiftDefinitionInactive, preview.Value.Invalid[0].Code);

        var applied = await harness.CopyScheduleWeek.ExecuteAsync(
            new CopyScheduleWeekCommand(WeekStart.AddDays(7), null, "actor", null),
            CancellationToken.None);
        Assert.False(applied.IsSuccess);
        Assert.Equal(ScheduleValidation.Codes.ScheduleCopyWeekBlocked, applied.Error!.Code);
        Assert.Single(harness.Store.ScheduleEntries);
    }

    [Fact]
    public async Task CopyWeek_CrossPropertyTargetRejected()
    {
        var harness = new WorkforceHarness();
        var day = await CreateDayShiftAsync(harness);
        var (employeeId, _) = await harness.SeedEmploymentAsync(WeekStart.AddDays(-14));
        Assert.True((await harness.UpsertSchedule.ExecuteAsync(
            ShiftCommand(employeeId, WeekStart, day.Id), CancellationToken.None)).IsSuccess);

        var transferDate = WeekStart.AddDays(7);
        var otherWorkplaceTransfer = new TransferEmployeeUseCase(
            harness.Store,
            harness.Clock,
            new FixedWorkplace(harness.OrganizationId, harness.OtherPropertyId));
        Assert.True((await otherWorkplaceTransfer.ExecuteAsync(
            new TransferEmployeeCommand(
                employeeId,
                harness.OtherPropertyDepartmentId,
                harness.OtherPropertyPositionId,
                transferDate),
            CancellationToken.None)).IsSuccess);

        var preview = await harness.CopyScheduleWeek.PreviewAsync(
            new CopyScheduleWeekCommand(transferDate, null, "actor", harness.PropertyId),
            CancellationToken.None);
        Assert.True(preview.IsSuccess, preview.Error?.Detail);
        Assert.True(preview.Value!.InvalidCount >= 1);
        Assert.Contains(
            preview.Value.Invalid,
            item => item.Code is ScheduleValidation.Codes.SchedulePropertyAccessDenied
                or ScheduleValidation.Codes.ScheduleCrossPropertyShift
                or ScheduleValidation.Codes.ScheduleDepartmentFilterDenied);
    }

    [Fact]
    public async Task CopyWeek_ReportsOverwriteCount()
    {
        var harness = new WorkforceHarness();
        var day = await CreateDayShiftAsync(harness);
        var eve = await harness.ShiftDefinitionAdmin.CreateAsync(
            new CreateShiftDefinitionCommand("aksam", "Akşam", new TimeOnly(16, 0), new TimeOnly(0, 0), true, 30, "actor"),
            CancellationToken.None);
        Assert.True(eve.IsSuccess, eve.Error?.Detail);
        var (employeeId, _) = await harness.SeedEmploymentAsync(WeekStart.AddDays(-14));

        Assert.True((await harness.UpsertSchedule.ExecuteAsync(
            ShiftCommand(employeeId, WeekStart, day.Id), CancellationToken.None)).IsSuccess);
        var targetWeek = WeekStart.AddDays(7);
        Assert.True((await harness.UpsertSchedule.ExecuteAsync(
            ShiftCommand(employeeId, targetWeek, eve.Value!.Id), CancellationToken.None)).IsSuccess);

        var preview = await harness.CopyScheduleWeek.PreviewAsync(
            new CopyScheduleWeekCommand(targetWeek, null, "actor", null),
            CancellationToken.None);
        Assert.True(preview.IsSuccess, preview.Error?.Detail);
        Assert.Equal(1, preview.Value!.CopyCount);
        Assert.Equal(1, preview.Value.OverwriteCount);
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
