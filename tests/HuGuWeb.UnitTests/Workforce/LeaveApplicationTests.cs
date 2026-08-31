using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class LeaveApplicationTests
{
    [Fact]
    public async Task EnsureDefaultLeaveTypes_IsIdempotent()
    {
        var harness = new WorkforceHarness();

        var first = await harness.EnsureDefaultLeaveTypes.ExecuteAsync(harness.OrganizationId, CancellationToken.None);
        var second = await harness.EnsureDefaultLeaveTypes.ExecuteAsync(harness.OrganizationId, CancellationToken.None);

        Assert.Equal(LeaveTypeDefaults.All.Count + 1, first);
        Assert.Equal(0, second);
        Assert.Equal(LeaveTypeDefaults.All.Count + 1, harness.Store.LeaveTypes.Count);
        var annual = harness.Store.LeaveTypes.Single(item => item.Code == "annual");
        Assert.True(annual.TracksBalance);
        Assert.Equal(LeaveTypeSystemKind.Annual, annual.SystemKind);
        var paternity = harness.Store.LeaveTypes.Single(item => item.Code == "paternity");
        Assert.Equal(10.0m, paternity.DefaultRequestAmount);
        var bereavement = harness.Store.LeaveTypes.Single(item => item.Code == "bereavement");
        Assert.Equal(3.0m, bereavement.DefaultRequestAmount);
        var birthday = harness.Store.LeaveTypes.Single(item => item.Code == LeaveTypeDefaults.OptionalCustom.BirthdayCode);
        Assert.Null(birthday.SystemKind);
        Assert.Equal(1.0m, birthday.DefaultRequestAmount);
        Assert.All(
            harness.Store.LeaveTypes.Where(item => item.Code != "annual"),
            item => Assert.False(item.TracksBalance));
    }

    [Fact]
    public async Task CreateLeaveType_DuplicateCode_IsRejected()
    {
        var harness = new WorkforceHarness();
        var created = await harness.LeaveTypeAdmin.CreateAsync(
            new CreateLeaveTypeCommand("study", "Study", TracksBalance: false, "actor"),
            CancellationToken.None);
        Assert.True(created.IsSuccess);

        var duplicate = await harness.LeaveTypeAdmin.CreateAsync(
            new CreateLeaveTypeCommand("STUDY", "Study Again", TracksBalance: false, "actor"),
            CancellationToken.None);

        Assert.False(duplicate.IsSuccess);
        Assert.Equal(LeaveValidation.Codes.LeaveTypeCodeConflict, duplicate.Error!.Code);
    }

    [Fact]
    public async Task Entitlement_ThenRecord_ProducesDerivedBalance()
    {
        var harness = new WorkforceHarness();
        var annual = harness.SeedLeaveType("annual", "Yıllık İzin", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, _) = await harness.SeedEmploymentAsync();

        var grant = await harness.RecordLeaveEntitlement.ExecuteAsync(
            new RecordLeaveEntitlementCommand(
                employeeId,
                EmploymentId: null,
                annual.Id,
                new DateOnly(2026, 1, 1),
                14.0m,
                LeaveEntitlementSource.Entitlement,
                Note: null,
                "actor"),
            CancellationToken.None);
        Assert.True(grant.IsSuccess, grant.Error?.Detail);

        var used = await harness.RecordLeave.ExecuteAsync(
            new RecordLeaveCommand(
                employeeId,
                EmploymentId: null,
                annual.Id,
                new DateOnly(2026, 8, 3),
                new DateOnly(2026, 8, 5),
                3.0m,
                Note: null,
                "actor"),
            CancellationToken.None);
        Assert.True(used.IsSuccess, used.Error?.Detail);

        var balance = used.Value!.Balances.Single(item => item.LeaveTypeId == annual.Id);
        Assert.Equal(14.0m, balance.NetMovement);
        Assert.Equal(3.0m, balance.Used);
        Assert.Equal(11.0m, balance.Remaining);
    }

    [Fact]
    public async Task Balance_CanGoNegative_AndIsVisible()
    {
        var harness = new WorkforceHarness();
        var annual = harness.SeedLeaveType("annual", "Yıllık İzin", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, _) = await harness.SeedEmploymentAsync();

        var used = await harness.RecordLeave.ExecuteAsync(
            new RecordLeaveCommand(
                employeeId,
                EmploymentId: null,
                annual.Id,
                new DateOnly(2026, 8, 3),
                new DateOnly(2026, 8, 5),
                3.0m,
                Note: null,
                "actor"),
            CancellationToken.None);

        Assert.True(used.IsSuccess, used.Error?.Detail);
        var balance = used.Value!.Balances.Single(item => item.LeaveTypeId == annual.Id);
        Assert.Equal(-3.0m, balance.Remaining);
    }

    [Fact]
    public async Task CancelledLeave_IsExcludedFromUsage()
    {
        var harness = new WorkforceHarness();
        var annual = harness.SeedLeaveType("annual", "Yıllık İzin", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, _) = await harness.SeedEmploymentAsync();

        await harness.RecordLeaveEntitlement.ExecuteAsync(
            new RecordLeaveEntitlementCommand(
                employeeId, null, annual.Id, new DateOnly(2026, 1, 1), 14.0m,
                LeaveEntitlementSource.Entitlement, null, "actor"),
            CancellationToken.None);

        var recorded = await harness.RecordLeave.ExecuteAsync(
            new RecordLeaveCommand(
                employeeId, null, annual.Id,
                new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 5), 3.0m, null, "actor"),
            CancellationToken.None);
        var recordId = recorded.Value!.Records.Single().Id;

        var cancelled = await harness.CancelLeaveRecord.ExecuteAsync(
            new CancelLeaveRecordCommand(employeeId, recordId, "mistake", "manager"),
            CancellationToken.None);

        Assert.True(cancelled.IsSuccess, cancelled.Error?.Detail);
        var balance = cancelled.Value!.Balances.Single(item => item.LeaveTypeId == annual.Id);
        Assert.Equal(14.0m, balance.Remaining);
        Assert.Equal(LeaveRecordStatus.Cancelled, cancelled.Value.Records.Single().Status);
    }

    [Fact]
    public async Task Record_OverlappingRecorded_IsRejected()
    {
        var harness = new WorkforceHarness();
        var annual = harness.SeedLeaveType("annual", "Yıllık İzin", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, _) = await harness.SeedEmploymentAsync();

        await harness.RecordLeave.ExecuteAsync(
            new RecordLeaveCommand(
                employeeId, null, annual.Id,
                new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 5), 3.0m, null, "actor"),
            CancellationToken.None);

        var overlap = await harness.RecordLeave.ExecuteAsync(
            new RecordLeaveCommand(
                employeeId, null, annual.Id,
                new DateOnly(2026, 8, 5), new DateOnly(2026, 8, 6), 2.0m, null, "actor"),
            CancellationToken.None);

        Assert.False(overlap.IsSuccess);
        Assert.Equal(LeaveValidation.Codes.LeaveOverlap, overlap.Error!.Code);
    }

    [Fact]
    public async Task Record_SameDateHalfDays_Overlap_IsRejected()
    {
        var harness = new WorkforceHarness();
        var annual = harness.SeedLeaveType("annual", "Yıllık İzin", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, _) = await harness.SeedEmploymentAsync();

        await harness.RecordLeave.ExecuteAsync(
            new RecordLeaveCommand(
                employeeId, null, annual.Id,
                new DateOnly(2026, 8, 4), new DateOnly(2026, 8, 4), 0.5m, null, "actor"),
            CancellationToken.None);

        var second = await harness.RecordLeave.ExecuteAsync(
            new RecordLeaveCommand(
                employeeId, null, annual.Id,
                new DateOnly(2026, 8, 4), new DateOnly(2026, 8, 4), 0.5m, null, "actor"),
            CancellationToken.None);

        Assert.False(second.IsSuccess);
        Assert.Equal(LeaveValidation.Codes.LeaveOverlap, second.Error!.Code);
    }

    [Fact]
    public async Task Record_BeforeEmploymentStart_IsRejected()
    {
        var harness = new WorkforceHarness();
        var annual = harness.SeedLeaveType("annual", "Yıllık İzin", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, _) = await harness.SeedEmploymentAsync(startDate: new DateOnly(2026, 7, 22));

        var result = await harness.RecordLeave.ExecuteAsync(
            new RecordLeaveCommand(
                employeeId, null, annual.Id,
                new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 2), 2.0m, null, "actor"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(LeaveValidation.Codes.LeaveDateOutsideEmployment, result.Error!.Code);
    }

    [Fact]
    public async Task Record_InactiveLeaveType_IsRejected()
    {
        var harness = new WorkforceHarness();
        var inactive = harness.SeedLeaveType("study", "Study", tracksBalance: false, active: false);
        var (employeeId, _) = await harness.SeedEmploymentAsync();

        var result = await harness.RecordLeave.ExecuteAsync(
            new RecordLeaveCommand(
                employeeId, null, inactive.Id,
                new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 4), 2.0m, null, "actor"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(LeaveValidation.Codes.LeaveTypeInactive, result.Error!.Code);
    }

    [Fact]
    public async Task Entitlement_ForNonBalanceType_IsRejected()
    {
        var harness = new WorkforceHarness();
        var unpaid = harness.SeedLeaveType("unpaid", "Ücretsiz İzin", tracksBalance: false, LeaveTypeSystemKind.Unpaid);
        var (employeeId, _) = await harness.SeedEmploymentAsync();

        var result = await harness.RecordLeaveEntitlement.ExecuteAsync(
            new RecordLeaveEntitlementCommand(
                employeeId, null, unpaid.Id, new DateOnly(2026, 1, 1), 5.0m,
                LeaveEntitlementSource.Entitlement, null, "actor"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(LeaveValidation.Codes.LeaveEntitlementBalanceNotSupported, result.Error!.Code);
    }

    [Fact]
    public async Task Cancel_AlreadyCancelled_IsRejected()
    {
        var harness = new WorkforceHarness();
        var annual = harness.SeedLeaveType("annual", "Yıllık İzin", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, _) = await harness.SeedEmploymentAsync();

        var recorded = await harness.RecordLeave.ExecuteAsync(
            new RecordLeaveCommand(
                employeeId, null, annual.Id,
                new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 5), 3.0m, null, "actor"),
            CancellationToken.None);
        var recordId = recorded.Value!.Records.Single().Id;

        await harness.CancelLeaveRecord.ExecuteAsync(
            new CancelLeaveRecordCommand(employeeId, recordId, "first", "manager"),
            CancellationToken.None);

        var again = await harness.CancelLeaveRecord.ExecuteAsync(
            new CancelLeaveRecordCommand(employeeId, recordId, "second", "manager"),
            CancellationToken.None);

        Assert.False(again.IsSuccess);
        Assert.Equal(LeaveValidation.Codes.LeaveAlreadyCancelled, again.Error!.Code);
    }

    [Fact]
    public async Task Cancel_BlankReason_IsRejected()
    {
        var harness = new WorkforceHarness();
        var annual = harness.SeedLeaveType("annual", "Yıllık İzin", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, _) = await harness.SeedEmploymentAsync();

        var recorded = await harness.RecordLeave.ExecuteAsync(
            new RecordLeaveCommand(
                employeeId, null, annual.Id,
                new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 5), 3.0m, null, "actor"),
            CancellationToken.None);
        var recordId = recorded.Value!.Records.Single().Id;

        var result = await harness.CancelLeaveRecord.ExecuteAsync(
            new CancelLeaveRecordCommand(employeeId, recordId, "   ", "manager"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(LeaveValidation.Codes.LeaveCancellationReasonRequired, result.Error!.Code);
    }

    [Fact]
    public async Task Overview_ForEmployeeWithNoLeaveData_ReturnsEmptyStateWithBalanceTrackedTypes()
    {
        var harness = new WorkforceHarness();
        harness.SeedLeaveType("annual", "Yıllık İzin", tracksBalance: true, LeaveTypeSystemKind.Annual);
        harness.SeedLeaveType("unpaid", "Ücretsiz İzin", tracksBalance: false, LeaveTypeSystemKind.Unpaid);
        var (employeeId, _) = await harness.SeedEmploymentAsync();

        var overview = await harness.LeaveQuery.ExecuteAsync(employeeId, null, CancellationToken.None);

        Assert.True(overview.IsSuccess, overview.Error?.Detail);
        Assert.Empty(overview.Value!.Records);
        Assert.Empty(overview.Value.Entitlements);
        var balance = Assert.Single(overview.Value.Balances);
        Assert.Equal("annual", balance.Code);
        Assert.Equal(0m, balance.Remaining);
    }

    [Fact]
    public async Task ListLeaveTypes_DoesNotCreateDatabaseState()
    {
        var harness = new WorkforceHarness();

        var listed = await harness.LeaveTypeAdmin.ListAsync(activeOnly: false, CancellationToken.None);

        Assert.True(listed.IsSuccess, listed.Error?.Detail);
        Assert.Empty(listed.Value!);
        Assert.Empty(harness.Store.LeaveTypes);
    }

    [Fact]
    public async Task EnsureDefaultLeaveTypes_ForAllOrganizations_IsIdempotent()
    {
        var harness = new WorkforceHarness();
        var secondOrg = Guid.CreateVersion7();
        harness.Store.Organizations.Add(new Organization(secondOrg, "Second"));

        var first = await harness.EnsureDefaultLeaveTypes.ExecuteForAllOrganizationsAsync(CancellationToken.None);
        var second = await harness.EnsureDefaultLeaveTypes.ExecuteForAllOrganizationsAsync(CancellationToken.None);

        Assert.Equal((LeaveTypeDefaults.All.Count + 1) * 2, first);
        Assert.Equal(0, second);
        Assert.Equal(
            LeaveTypeDefaults.All.Count + 1,
            harness.Store.LeaveTypes.Count(item => item.OrganizationId == harness.OrganizationId));
        Assert.Equal(
            LeaveTypeDefaults.All.Count + 1,
            harness.Store.LeaveTypes.Count(item => item.OrganizationId == secondOrg));
    }

    [Fact]
    public async Task Record_AfterEmploymentEndDate_IsRejected()
    {
        var harness = new WorkforceHarness();
        var annual = harness.SeedLeaveType("annual", "Yıllık İzin", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, _) = await harness.SeedEmploymentAsync(startDate: new DateOnly(2026, 1, 1));
        var ended = await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(employeeId, new DateOnly(2026, 6, 30), EmploymentTerminationReason.Resignation),
            CancellationToken.None);
        Assert.True(ended.IsSuccess, ended.Error?.Detail);

        var result = await harness.RecordLeave.ExecuteAsync(
            new RecordLeaveCommand(
                employeeId, null, annual.Id,
                new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 2), 2.0m, null, "actor"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(LeaveValidation.Codes.LeaveDateOutsideEmployment, result.Error!.Code);
    }

    [Fact]
    public async Task Record_OnEndedEmployment_WithinPeriod_IsAllowed()
    {
        var harness = new WorkforceHarness();
        var annual = harness.SeedLeaveType("annual", "Yıllık İzin", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, _) = await harness.SeedEmploymentAsync(startDate: new DateOnly(2026, 1, 1));
        var ended = await harness.EndEmployment.ExecuteAsync(
            new EndEmploymentCommand(employeeId, new DateOnly(2026, 6, 30), EmploymentTerminationReason.Resignation),
            CancellationToken.None);
        Assert.True(ended.IsSuccess, ended.Error?.Detail);

        var result = await harness.RecordLeave.ExecuteAsync(
            new RecordLeaveCommand(
                employeeId, null, annual.Id,
                new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 3), 3.0m, null, "actor"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Detail);
        Assert.Single(result.Value!.Records);
    }

    [Fact]
    public async Task UpdateTracksBalance_AfterHistoricalUsage_IsRejected()
    {
        var harness = new WorkforceHarness();
        var created = await harness.LeaveTypeAdmin.CreateAsync(
            new CreateLeaveTypeCommand("study", "Study", TracksBalance: true, "actor"),
            CancellationToken.None);
        Assert.True(created.IsSuccess);
        var (employeeId, _) = await harness.SeedEmploymentAsync();
        await harness.RecordLeaveEntitlement.ExecuteAsync(
            new RecordLeaveEntitlementCommand(
                employeeId, null, created.Value!.Id, new DateOnly(2026, 1, 1), 2.0m,
                LeaveEntitlementSource.Entitlement, null, "actor"),
            CancellationToken.None);

        var updated = await harness.LeaveTypeAdmin.UpdateAsync(
            new UpdateLeaveTypeCommand(created.Value.Id, Name: null, TracksBalance: false, IsActive: null, "actor"),
            CancellationToken.None);

        Assert.False(updated.IsSuccess);
        Assert.Equal(LeaveValidation.Codes.LeaveTypeHasHistory, updated.Error!.Code);
    }

    [Fact]
    public async Task InactiveLeaveType_RemainsReadableInOverview()
    {
        var harness = new WorkforceHarness();
        var unpaid = harness.SeedLeaveType("unpaid", "Ücretsiz İzin", tracksBalance: false, LeaveTypeSystemKind.Unpaid);
        var (employeeId, _) = await harness.SeedEmploymentAsync();
        await harness.RecordLeave.ExecuteAsync(
            new RecordLeaveCommand(
                employeeId, null, unpaid.Id,
                new DateOnly(2026, 8, 3), new DateOnly(2026, 8, 3), 1.0m, null, "actor"),
            CancellationToken.None);
        unpaid.Deactivate("actor", harness.Clock.UtcNow);

        var overview = await harness.LeaveQuery.ExecuteAsync(employeeId, null, CancellationToken.None);

        Assert.True(overview.IsSuccess, overview.Error?.Detail);
        Assert.Contains(overview.Value!.LeaveTypes, item => item.Id == unpaid.Id && !item.IsActive);
        Assert.Equal(unpaid.Id, overview.Value.Records.Single().LeaveTypeId);
    }
}
