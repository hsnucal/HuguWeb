using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class LeaveRequestApiSliceTests
{
    private static readonly DateOnly Mon = new(2026, 8, 10);
    private static readonly DateOnly Tue = new(2026, 8, 11);
    private static readonly DateOnly Wed = new(2026, 8, 12);
    private static readonly DateOnly Thu = new(2026, 8, 13);
    private static readonly DateOnly Fri = new(2026, 8, 14);

    [Fact]
    public void SchedulePreview_SuggestedAmount_IgnoresRestAndUnscheduled()
    {
        var employmentId = Guid.CreateVersion7();
        var assignmentId = Guid.CreateVersion7();
        var shiftId = Guid.CreateVersion7();
        Assert.True(ScheduleEntry.TryCreateShift(
            Guid.CreateVersion7(), employmentId, assignmentId, Mon, shiftId, null, "a", DateTimeOffset.UtcNow,
            out var mon, out _, out _));
        Assert.True(ScheduleEntry.TryCreateShift(
            Guid.CreateVersion7(), employmentId, assignmentId, Tue, shiftId, null, "a", DateTimeOffset.UtcNow,
            out var tue, out _, out _));
        Assert.True(ScheduleEntry.TryCreateRestDay(
            Guid.CreateVersion7(), employmentId, assignmentId, Wed, null, "a", DateTimeOffset.UtcNow,
            out var wed, out _, out _));
        Assert.True(ScheduleEntry.TryCreateShift(
            Guid.CreateVersion7(), employmentId, assignmentId, Thu, shiftId, null, "a", DateTimeOffset.UtcNow,
            out var thu, out _, out _));

        var preview = LeaveSchedulePreview.Build(Mon, Fri, [mon!, tue!, wed!, thu!]);

        Assert.Equal(3.0m, preview.SuggestedAmount);
        Assert.True(preview.ScheduleIncomplete);
        Assert.Equal(LeaveSchedulePreview.StateUnscheduled, preview.Days.Single(item => item.Date == Fri).State);
        Assert.Equal(0m, preview.Days.Single(item => item.Date == Wed).ChargeableCandidate);
    }

    [Fact]
    public async Task SelfService_CreateListDetailWithdraw_OwnOnly()
    {
        var harness = new WorkforceHarness();
        var leaveType = harness.SeedLeaveType("annual", "Annual", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeA, _) = await harness.SeedEmploymentAsync();
        var (employeeB, _) = await harness.SeedEmploymentAsync();

        var created = await harness.CreateMyLeaveRequest.ExecuteAsync(
            new CreateMyLeaveRequestCommand(
                employeeA, leaveType.Id, Mon, Wed, 2.0m, "trip", "actor"),
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error?.Detail);

        var noLink = await harness.CreateMyLeaveRequest.ExecuteAsync(
            new CreateMyLeaveRequestCommand(
                null, leaveType.Id, Mon, Wed, 2.0m, null, "actor"),
            CancellationToken.None);
        Assert.Equal(LeaveValidation.Codes.LeaveRequestAccountLinkRequired, noLink.Error!.Code);

        var listA = await harness.LeaveRequestQuery.ListMineAsync(employeeA, 1, 50, CancellationToken.None);
        Assert.True(listA.IsSuccess);
        Assert.Single(listA.Value!.Items);

        var stolen = await harness.LeaveRequestQuery.GetMineAsync(
            employeeB, created.Value!.Id, CancellationToken.None);
        Assert.Equal(LeaveValidation.Codes.LeaveRequestNotOwned, stolen.Error!.Code);

        var withdrawn = await harness.LeaveRequestActions.WithdrawMineAsync(
            employeeA, created.Value.Id, null, "actor", CancellationToken.None);
        Assert.True(withdrawn.IsSuccess, withdrawn.Error?.Detail);
        Assert.Equal(LeaveRequestStatus.Cancelled, withdrawn.Value!.Status);
    }

    [Fact]
    public async Task SelfService_Preview_ReportsIncompleteAndBalanceWarning()
    {
        var harness = new WorkforceHarness();
        var leaveType = harness.SeedLeaveType("annual", "Annual", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, _) = await harness.SeedEmploymentAsync();
        await harness.RecordLeaveEntitlement.ExecuteAsync(
            new RecordLeaveEntitlementCommand(
                employeeId, null, leaveType.Id, new DateOnly(2026, 1, 1), 1.0m,
                LeaveEntitlementSource.Entitlement, null, "actor"),
            CancellationToken.None);

        var preview = await harness.PreviewLeaveRequest.ExecuteMineAsync(
            new PreviewMyLeaveRequestCommand(employeeId, leaveType.Id, Mon, Fri, 5.0m),
            CancellationToken.None);

        Assert.True(preview.IsSuccess, preview.Error?.Detail);
        Assert.True(preview.Value!.ScheduleIncomplete);
        Assert.Contains("leave-request-schedule-incomplete", preview.Value.Warnings);
        Assert.NotNull(preview.Value.Balance);
        Assert.True(preview.Value.Balance!.IsNegativeProjected);
        Assert.Contains("leave-request-balance-overrun", preview.Value.Warnings);
    }

    [Fact]
    public async Task DepartmentScope_FiltersHistoricalAssignment_NotCurrentTransfer()
    {
        var harness = new WorkforceHarness();
        var leaveType = harness.SeedLeaveType("annual", "Annual", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, employmentId) = await harness.SeedEmploymentAsync(startDate: new DateOnly(2026, 7, 1));

        var created = await harness.CreateLeaveRequest.ExecuteAsync(
            new CreateLeaveRequestCommand(
                employeeId, employmentId, leaveType.Id, Mon, Tue, 2.0m, null, "actor"),
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error?.Detail);
        var housekeepingAssignmentId = created.Value!.AssignmentId;
        var housekeepingDept = harness.DepartmentId;

        Assert.True((await harness.Transfer.ExecuteAsync(
            new TransferEmployeeCommand(
                employeeId, harness.OtherDepartmentId, harness.OtherPositionId, Wed),
            CancellationToken.None)).IsSuccess);

        var allowedHousekeeping = new HashSet<Guid> { housekeepingDept };
        var detail = await harness.LeaveRequestQuery.GetManagedAsync(
            created.Value.Id,
            harness.PropertyId,
            allowedHousekeeping,
            CancellationToken.None);
        Assert.True(detail.IsSuccess, detail.Error?.Detail);
        Assert.Equal(housekeepingAssignmentId, detail.Value!.AssignmentId);
        Assert.Equal(housekeepingDept, detail.Value.DepartmentId);

        var deniedFrontOffice = await harness.LeaveRequestQuery.GetManagedAsync(
            created.Value.Id,
            harness.PropertyId,
            new HashSet<Guid> { harness.OtherDepartmentId },
            CancellationToken.None);
        Assert.Equal(
            LeaveValidation.Codes.LeaveRequestDepartmentAccessDenied,
            deniedFrontOffice.Error!.Code);

        var list = await harness.LeaveRequestQuery.ListManagedAsync(
            new LeaveRequestListFilter(
                harness.PropertyId,
                new HashSet<Guid> { harness.OtherDepartmentId },
                null, null, null, null, null, null, null, 1, 50),
            CancellationToken.None);
        Assert.Empty(list.Value!.Items);
        Assert.Equal(0, list.Value.TotalCount);
    }

    [Fact]
    public async Task DepartmentApprove_RequiresScope_CreatesNoLeaveRecord()
    {
        var harness = await SeedPendingAsync();
        var requestId = harness.Store.LeaveRequests.Single().Id;

        var denied = await harness.LeaveRequestActions.DepartmentApproveAsync(
            requestId, null, "dept", harness.PropertyId,
            new HashSet<Guid> { harness.OtherDepartmentId },
            canApprove: true,
            CancellationToken.None);
        Assert.Equal(LeaveValidation.Codes.LeaveRequestDepartmentAccessDenied, denied.Error!.Code);

        var noPerm = await harness.LeaveRequestActions.DepartmentApproveAsync(
            requestId, null, "dept", harness.PropertyId, allowedDepartmentIds: null,
            canApprove: false,
            CancellationToken.None);
        Assert.Equal(LeaveValidation.Codes.LeaveRequestApprovalPermissionDenied, noPerm.Error!.Code);

        var approved = await harness.LeaveRequestActions.DepartmentApproveAsync(
            requestId, null, "dept", harness.PropertyId, allowedDepartmentIds: null,
            canApprove: true,
            CancellationToken.None);
        Assert.True(approved.IsSuccess, approved.Error?.Detail);
        Assert.Equal(LeaveRequestApprovalStage.Hr, approved.Value!.Request.ApprovalStage);
        Assert.Empty(harness.Store.LeaveRecords);
    }

    [Fact]
    public async Task HrApprove_RequiresManage_CreatesOneRecord_DoubleBlocked()
    {
        var harness = await SeedPendingAsync();
        var requestId = harness.Store.LeaveRequests.Single().Id;
        Assert.True((await harness.LeaveRequestActions.DepartmentApproveAsync(
            requestId, null, "dept", harness.PropertyId, null, true, CancellationToken.None)).IsSuccess);

        var noManage = await harness.LeaveRequestActions.HrApproveAsync(
            requestId, 2.0m, null, "hr", harness.PropertyId, null,
            canManage: false, CancellationToken.None);
        Assert.Equal(LeaveValidation.Codes.LeaveRequestApprovalPermissionDenied, noManage.Error!.Code);

        var first = await harness.LeaveRequestActions.HrApproveAsync(
            requestId, 2.0m, "ok", "hr", harness.PropertyId, null,
            canManage: true, CancellationToken.None);
        Assert.True(first.IsSuccess, first.Error?.Detail);
        Assert.Single(harness.Store.LeaveRecords);
        Assert.Equal(2.0m, first.Value!.Request.FinalAmount);

        var second = await harness.LeaveRequestActions.HrApproveAsync(
            requestId, 2.0m, null, "hr", harness.PropertyId, null,
            canManage: true, CancellationToken.None);
        Assert.Equal(LeaveValidation.Codes.LeaveRequestAlreadyFinalized, second.Error!.Code);
        Assert.Single(harness.Store.LeaveRecords);
    }

    [Fact]
    public async Task StageSecurity_WrongTransitionsRejected()
    {
        var harness = await SeedPendingAsync();
        var requestId = harness.Store.LeaveRequests.Single().Id;

        var hrTooEarly = await harness.LeaveRequestActions.HrApproveAsync(
            requestId, 2.0m, null, "hr", harness.PropertyId, null, true, CancellationToken.None);
        Assert.Equal(LeaveValidation.Codes.LeaveRequestInvalidApprovalStage, hrTooEarly.Error!.Code);

        Assert.True((await harness.LeaveRequestActions.DepartmentApproveAsync(
            requestId, null, "dept", harness.PropertyId, null, true, CancellationToken.None)).IsSuccess);

        var deptTooLate = await harness.LeaveRequestActions.DepartmentApproveAsync(
            requestId, null, "dept", harness.PropertyId, null, true, CancellationToken.None);
        Assert.Equal(LeaveValidation.Codes.LeaveRequestInvalidApprovalStage, deptTooLate.Error!.Code);

        var cancelPending = await harness.LeaveRequestActions.CancelApprovedAsync(
            requestId, "x", "hr", harness.PropertyId, null, true, CancellationToken.None);
        Assert.False(cancelPending.IsSuccess);
    }

    [Fact]
    public async Task Reject_StagePermissionSplit()
    {
        var harness = await SeedPendingAsync();
        var requestId = harness.Store.LeaveRequests.Single().Id;

        var deptRejectWithoutPerm = await harness.LeaveRequestActions.RejectAsync(
            requestId, "no", "actor", harness.PropertyId, null,
            canApprove: false, canManage: false, CancellationToken.None);
        Assert.Equal(
            LeaveValidation.Codes.LeaveRequestApprovalPermissionDenied,
            deptRejectWithoutPerm.Error!.Code);

        Assert.True((await harness.LeaveRequestActions.DepartmentApproveAsync(
            requestId, null, "dept", harness.PropertyId, null, true, CancellationToken.None)).IsSuccess);

        var hrRejectWithApproveOnly = await harness.LeaveRequestActions.RejectAsync(
            requestId, "no", "actor", harness.PropertyId, null,
            canApprove: true, canManage: false, CancellationToken.None);
        Assert.Equal(
            LeaveValidation.Codes.LeaveRequestApprovalPermissionDenied,
            hrRejectWithApproveOnly.Error!.Code);

        var hrReject = await harness.LeaveRequestActions.RejectAsync(
            requestId, "no", "hr", harness.PropertyId, null,
            canApprove: true, canManage: true, CancellationToken.None);
        Assert.True(hrReject.IsSuccess, hrReject.Error?.Detail);
        Assert.Equal(LeaveRequestStatus.Rejected, hrReject.Value!.Request.Status);
        Assert.Empty(harness.Store.LeaveRecords);
    }

    [Fact]
    public async Task HrApprove_RefreshesSchedulePreview_UnscheduledAllowed()
    {
        var harness = await SeedPendingAsync();
        var requestId = harness.Store.LeaveRequests.Single().Id;
        var shift = await harness.ShiftDefinitionAdmin.CreateAsync(
            new CreateShiftDefinitionCommand("G", "Gündüz", new TimeOnly(8, 0), new TimeOnly(16, 0), false, 0, "actor"),
            CancellationToken.None);
        Assert.True(shift.IsSuccess, shift.Error?.Detail);
        var employeeId = harness.Store.Employments.Single().EmployeeId;

        Assert.True((await harness.UpsertSchedule.ExecuteAsync(
            new UpsertScheduleEntryCommand(
                employeeId, Mon, ScheduleEntryKind.Shift, shift.Value!.Id, null, "actor",
                harness.PropertyId, null),
            CancellationToken.None)).IsSuccess);
        // Tue left Unscheduled on purpose

        Assert.True((await harness.LeaveRequestActions.DepartmentApproveAsync(
            requestId, null, "dept", harness.PropertyId, null, true, CancellationToken.None)).IsSuccess);

        var detail = await harness.LeaveRequestQuery.GetManagedAsync(
            requestId, harness.PropertyId, null, CancellationToken.None);
        Assert.True(detail.IsSuccess, detail.Error?.Detail);
        Assert.True(detail.Value!.ScheduleIncomplete);
        Assert.Contains("leave-request-schedule-incomplete", detail.Value.Warnings);
        Assert.Equal(1.0m, detail.Value.SuggestedAmount);
        Assert.Equal(2.0m, detail.Value.RequestedAmount);

        var approved = await harness.LeaveRequestActions.HrApproveAsync(
            requestId, 1.5m, null, "hr", harness.PropertyId, null, true, CancellationToken.None);
        Assert.True(approved.IsSuccess, approved.Error?.Detail);
        Assert.Equal(1.5m, harness.Store.LeaveRecords.Single().Amount);
    }

    [Fact]
    public async Task ListManaged_DepartmentScope_IncludesOwnedAssignment_ExcludesOthers()
    {
        var harness = await SeedPendingAsync();
        var requestId = harness.Store.LeaveRequests.Single().Id;

        var engVisible = await harness.LeaveRequestQuery.ListManagedAsync(
            new LeaveRequestListFilter(
                harness.PropertyId,
                new HashSet<Guid> { harness.DepartmentId },
                LeaveRequestStatus.Pending,
                LeaveRequestApprovalStage.Department,
                null, null, null, null, null, 1, 50),
            CancellationToken.None);
        Assert.True(engVisible.IsSuccess);
        Assert.Contains(engVisible.Value!.Items, item => item.Id == requestId);

        var otherHidden = await harness.LeaveRequestQuery.ListManagedAsync(
            new LeaveRequestListFilter(
                harness.PropertyId,
                new HashSet<Guid> { harness.OtherDepartmentId },
                LeaveRequestStatus.Pending,
                LeaveRequestApprovalStage.Department,
                null, null, null, null, null, 1, 50),
            CancellationToken.None);
        Assert.True(otherHidden.IsSuccess);
        Assert.DoesNotContain(otherHidden.Value!.Items, item => item.Id == requestId);
    }

    [Fact]
    public async Task HrList_SeesDepartmentStage_ButCannotFinalApproveUntilHrStage()
    {
        var harness = await SeedPendingAsync();
        var requestId = harness.Store.LeaveRequests.Single().Id;

        var list = await harness.LeaveRequestQuery.ListManagedAsync(
            new LeaveRequestListFilter(
                harness.PropertyId,
                AllowedDepartmentIds: null,
                LeaveRequestStatus.Pending,
                null,
                null, null, null, null, null, 1, 50),
            CancellationToken.None);
        Assert.Contains(list.Value!.Items, item =>
            item.Id == requestId && item.ApprovalStage == LeaveRequestApprovalStage.Department);

        var hrTooEarly = await harness.LeaveRequestActions.HrApproveAsync(
            requestId, 2.0m, null, "hr", harness.PropertyId, null, canManage: true, CancellationToken.None);
        Assert.Equal(LeaveValidation.Codes.LeaveRequestInvalidApprovalStage, hrTooEarly.Error!.Code);
        Assert.Empty(harness.Store.LeaveRecords);
    }

    [Fact]
    public async Task CrossProperty_AccessDenied()
    {
        var harness = await SeedPendingAsync();
        var requestId = harness.Store.LeaveRequests.Single().Id;

        var denied = await harness.LeaveRequestQuery.GetManagedAsync(
            requestId,
            harness.OtherPropertyId,
            allowedDepartmentIds: null,
            CancellationToken.None);
        Assert.Equal(LeaveValidation.Codes.LeaveRequestDepartmentAccessDenied, denied.Error!.Code);
    }

    [Fact]
    public async Task WithdrawApproved_Blocked_CancelApproved_Works()
    {
        var harness = await SeedPendingAsync();
        var requestId = harness.Store.LeaveRequests.Single().Id;
        var employeeId = harness.Store.Employments.Single().EmployeeId;
        Assert.True((await harness.LeaveRequestActions.DepartmentApproveAsync(
            requestId, null, "dept", harness.PropertyId, null, true, CancellationToken.None)).IsSuccess);
        Assert.True((await harness.LeaveRequestActions.HrApproveAsync(
            requestId, 2.0m, null, "hr", harness.PropertyId, null, true, CancellationToken.None)).IsSuccess);

        var withdraw = await harness.LeaveRequestActions.WithdrawMineAsync(
            employeeId, requestId, null, "actor", CancellationToken.None);
        Assert.Equal(LeaveValidation.Codes.LeaveRequestAlreadyFinalized, withdraw.Error!.Code);

        var cancelled = await harness.LeaveRequestActions.CancelApprovedAsync(
            requestId, "mistake", "hr", harness.PropertyId, null, true, CancellationToken.None);
        Assert.True(cancelled.IsSuccess, cancelled.Error?.Detail);
        Assert.Equal(LeaveRequestStatus.Cancelled, cancelled.Value!.Request.Status);
        Assert.Equal(LeaveRecordStatus.Cancelled, harness.Store.LeaveRecords.Single().Status);
    }

    private static async Task<WorkforceHarness> SeedPendingAsync()
    {
        var harness = new WorkforceHarness();
        var leaveType = harness.SeedLeaveType("annual", "Annual", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, employmentId) = await harness.SeedEmploymentAsync();
        var created = await harness.CreateLeaveRequest.ExecuteAsync(
            new CreateLeaveRequestCommand(
                employeeId, employmentId, leaveType.Id, Mon, Tue, 2.0m, null, "actor"),
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error?.Detail);
        return harness;
    }
}
