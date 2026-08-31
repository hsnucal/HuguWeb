using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.UnitTests.Workforce;

public class LeaveRequestTests
{
    private static readonly DateOnly Start = new(2026, 8, 10);
    private static readonly DateOnly End = new(2026, 8, 12);
    private static readonly DateTimeOffset Now = new(2026, 8, 21, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TryCreate_Valid_StartsPendingDepartment()
    {
        Assert.True(TryCreateRequest(Start, End, 3.0m, out var request, out _));
        Assert.Equal(LeaveRequestStatus.Pending, request!.Status);
        Assert.Equal(LeaveRequestApprovalStage.Department, request.ApprovalStage);
    }

    [Fact]
    public void TryCreate_HalfDay_Succeeds()
    {
        Assert.True(TryCreateRequest(Start, Start, 0.5m, out var request, out _));
        Assert.Equal(0.5m, request!.RequestedAmount);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(0.25)]
    public void TryCreate_InvalidAmount_IsRejected(double amount)
    {
        Assert.False(TryCreateRequest(Start, End, (decimal)amount, out _, out var errorCode));
        Assert.Equal(LeaveValidation.Codes.LeaveRequestInvalidAmount, errorCode);
    }

    [Fact]
    public void TryCreate_EndBeforeStart_IsRejected()
    {
        Assert.False(TryCreateRequest(End, Start, 1.0m, out _, out var errorCode));
        Assert.Equal(LeaveValidation.Codes.LeaveInvalidDateRange, errorCode);
    }

    [Fact]
    public void Assignment_SameRange_Accepted()
    {
        var assignment = Assignment.StartPrimary(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Start);
        Assert.True(LeaveRequestAssignment.TryResolveForRange(
            [assignment], Start, End, out var resolved, out _));
        Assert.Equal(assignment.Id, resolved!.Id);
    }

    [Fact]
    public void Assignment_CrossBoundary_Rejected()
    {
        var first = Assignment.StartPrimary(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Start);
        Assert.True(first.TryCloseOn(Start.AddDays(1), out _));
        var second = Assignment.StartPrimary(
            Guid.CreateVersion7(), first.EmploymentId, Guid.CreateVersion7(), Guid.CreateVersion7(), Start.AddDays(2));

        Assert.False(LeaveRequestAssignment.TryResolveForRange(
            [first, second], Start, End, out _, out var errorCode));
        Assert.Equal(LeaveValidation.Codes.LeaveRequestCrossAssignmentRange, errorCode);
    }

    [Fact]
    public void Assignment_MissingCoveringStart_Rejected()
    {
        var assignment = Assignment.StartPrimary(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), End);
        Assert.False(LeaveRequestAssignment.TryResolveForRange(
            [assignment], Start, End, out _, out var errorCode));
        Assert.Equal(LeaveValidation.Codes.LeaveRequestAssignmentNotFound, errorCode);
    }

    [Fact]
    public void Overlap_PendingBlocks_RejectedIgnored_CancelledIgnored()
    {
        Assert.True(TryCreateRequest(Start, End, 3.0m, out var pending, out _));
        Assert.True(TryCreateRequest(Start.AddDays(10), End.AddDays(10), 3.0m, out var rejected, out _));
        Assert.True(rejected!.TryReject("actor", Now, null, out _, out _));
        Assert.True(TryCreateRequest(Start.AddDays(20), End.AddDays(20), 3.0m, out var cancelled, out _));
        Assert.True(cancelled!.TryWithdraw("actor", Now, null, out _, out _));

        Assert.True(LeaveRequestOverlap.OverlapsAnyActiveRequest([pending!, rejected, cancelled], Start, End));
        Assert.False(LeaveRequestOverlap.OverlapsAnyActiveRequest([rejected, cancelled], Start, End));
    }

    [Fact]
    public void Overlap_SameDayHalfDay_Blocked()
    {
        Assert.True(TryCreateRequest(Start, Start, 0.5m, out var first, out _));
        Assert.True(LeaveRequestOverlap.OverlapsAnyActiveRequest([first!], Start, Start));
    }

    [Fact]
    public void Overlap_RecordedBlocks_CancelledRecordIgnored()
    {
        Assert.True(LeaveRecord.TryCreate(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
            Start, End, 3.0m, null, "actor", Now, out var recorded, out _, out _));
        Assert.True(LeaveRequestOverlap.BlocksCreateOrApprove([], [recorded!], Start, End));
        Assert.True(recorded!.TryCancel("done", "actor", Now, out _, out _));
        Assert.False(LeaveRequestOverlap.BlocksCreateOrApprove([], [recorded], Start, End));
    }

    [Fact]
    public void State_DepartmentApprove_ThenHrApprove_ThenNoReopen()
    {
        Assert.True(TryCreateRequest(Start, End, 3.0m, out var request, out _));
        Assert.True(request!.TryApproveDepartment("dept", Now, out var deptDecision, out _));
        Assert.Equal(LeaveRequestStatus.Pending, request.Status);
        Assert.Equal(LeaveRequestApprovalStage.Hr, request.ApprovalStage);
        Assert.Equal(LeaveRequestDecisionKind.Approved, deptDecision!.Decision);

        Assert.True(request.TryApproveHr("hr", Now.AddMinutes(1), null, out var hrDecision, out _));
        Assert.Equal(LeaveRequestStatus.Approved, request.Status);
        Assert.Equal(LeaveRequestApprovalStage.Done, request.ApprovalStage);
        Assert.Equal(LeaveRequestDecisionKind.Approved, hrDecision!.Decision);

        Assert.False(request.TryReject("actor", Now, null, out _, out var reopenError));
        Assert.Equal(LeaveValidation.Codes.LeaveRequestAlreadyFinalized, reopenError);
    }

    [Fact]
    public void State_RejectDepartment_AndRejectHr()
    {
        Assert.True(TryCreateRequest(Start, End, 3.0m, out var dept, out _));
        Assert.True(dept!.TryReject("actor", Now, "no", out _, out _));
        Assert.Equal(LeaveRequestStatus.Rejected, dept.Status);
        Assert.Equal(LeaveRequestApprovalStage.Done, dept.ApprovalStage);

        Assert.True(TryCreateRequest(Start, End, 3.0m, out var hr, out _));
        Assert.True(hr!.TryApproveDepartment("dept", Now, out _, out _));
        Assert.True(hr.TryReject("hr", Now, "no", out _, out _));
        Assert.Equal(LeaveRequestStatus.Rejected, hr.Status);
    }

    [Fact]
    public void State_WithdrawPending_WithdrawApprovedBlocked()
    {
        Assert.True(TryCreateRequest(Start, End, 3.0m, out var pending, out _));
        Assert.True(pending!.TryWithdraw("actor", Now, null, out _, out _));
        Assert.Equal(LeaveRequestStatus.Cancelled, pending.Status);

        Assert.True(TryCreateRequest(Start, End, 3.0m, out var approved, out _));
        Assert.True(approved!.TryApproveDepartment("dept", Now, out _, out _));
        Assert.True(approved.TryApproveHr("hr", Now, null, out _, out _));
        Assert.False(approved.TryWithdraw("actor", Now, null, out _, out var errorCode));
        Assert.Equal(LeaveValidation.Codes.LeaveRequestAlreadyFinalized, errorCode);
    }

    [Fact]
    public void State_InvalidStageTransitions()
    {
        Assert.True(TryCreateRequest(Start, End, 3.0m, out var request, out _));
        Assert.False(request!.TryApproveHr("hr", Now, null, out _, out var hrError));
        Assert.Equal(LeaveValidation.Codes.LeaveRequestInvalidApprovalStage, hrError);

        Assert.True(request.TryApproveDepartment("dept", Now, out _, out _));
        Assert.False(request.TryApproveDepartment("dept", Now, out _, out var deptError));
        Assert.Equal(LeaveValidation.Codes.LeaveRequestInvalidApprovalStage, deptError);
    }

    [Fact]
    public async Task Create_ValidEmployment_ResolvesAssignmentServerSide()
    {
        var harness = new WorkforceHarness();
        var leaveType = harness.SeedLeaveType("annual", "Annual", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, employmentId) = await harness.SeedEmploymentAsync();
        var assignmentId = harness.Store.Assignments.Single(item => item.EmploymentId == employmentId).Id;

        var created = await harness.CreateLeaveRequest.ExecuteAsync(
            new CreateLeaveRequestCommand(
                employeeId, employmentId, leaveType.Id, Start, End, 3.0m, "trip", "actor"),
            CancellationToken.None);

        Assert.True(created.IsSuccess, created.Error?.Detail);
        Assert.Equal(assignmentId, created.Value!.AssignmentId);
        Assert.Equal(LeaveRequestStatus.Pending, created.Value.Status);
        Assert.Equal(LeaveRequestApprovalStage.Department, created.Value.ApprovalStage);
    }

    [Fact]
    public async Task Create_OutsideEmployment_Rejected()
    {
        var harness = new WorkforceHarness();
        var leaveType = harness.SeedLeaveType("annual", "Annual", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, employmentId) = await harness.SeedEmploymentAsync(startDate: new DateOnly(2026, 8, 1));

        var before = await harness.CreateLeaveRequest.ExecuteAsync(
            new CreateLeaveRequestCommand(
                employeeId, employmentId, leaveType.Id,
                new DateOnly(2026, 7, 20), new DateOnly(2026, 7, 22), 3.0m, null, "actor"),
            CancellationToken.None);
        Assert.False(before.IsSuccess);
        Assert.Equal(LeaveValidation.Codes.LeaveRequestDateOutsideEmployment, before.Error!.Code);
    }

    [Fact]
    public async Task Create_InactiveLeaveType_Rejected()
    {
        var harness = new WorkforceHarness();
        var leaveType = harness.SeedLeaveType("custom", "Custom", tracksBalance: false, active: false);
        var (employeeId, employmentId) = await harness.SeedEmploymentAsync();

        var created = await harness.CreateLeaveRequest.ExecuteAsync(
            new CreateLeaveRequestCommand(
                employeeId, employmentId, leaveType.Id, Start, End, 3.0m, null, "actor"),
            CancellationToken.None);

        Assert.False(created.IsSuccess);
        Assert.Equal(LeaveValidation.Codes.LeaveRequestTypeInactive, created.Error!.Code);
    }

    [Fact]
    public async Task Create_CrossAssignmentRange_Rejected()
    {
        var harness = new WorkforceHarness();
        var leaveType = harness.SeedLeaveType("annual", "Annual", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, employmentId) = await harness.SeedEmploymentAsync(startDate: new DateOnly(2026, 7, 1));
        var transferDate = new DateOnly(2026, 8, 11);
        Assert.True((await harness.Transfer.ExecuteAsync(
            new TransferEmployeeCommand(
                employeeId, harness.OtherDepartmentId, harness.OtherPositionId, transferDate),
            CancellationToken.None)).IsSuccess);

        var created = await harness.CreateLeaveRequest.ExecuteAsync(
            new CreateLeaveRequestCommand(
                employeeId, employmentId, leaveType.Id, Start, End, 3.0m, null, "actor"),
            CancellationToken.None);

        Assert.False(created.IsSuccess);
        Assert.Equal(LeaveValidation.Codes.LeaveRequestCrossAssignmentRange, created.Error!.Code);
    }

    [Fact]
    public async Task Create_OverlapPendingAndRecorded_Rejected_RejectedIgnored()
    {
        var harness = new WorkforceHarness();
        var leaveType = harness.SeedLeaveType("annual", "Annual", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, employmentId) = await harness.SeedEmploymentAsync();

        Assert.True((await harness.CreateLeaveRequest.ExecuteAsync(
            new CreateLeaveRequestCommand(
                employeeId, employmentId, leaveType.Id, Start, End, 3.0m, null, "actor"),
            CancellationToken.None)).IsSuccess);

        var blockedPending = await harness.CreateLeaveRequest.ExecuteAsync(
            new CreateLeaveRequestCommand(
                employeeId, employmentId, leaveType.Id, End, End.AddDays(2), 2.0m, null, "actor"),
            CancellationToken.None);
        Assert.Equal(LeaveValidation.Codes.LeaveRequestOverlap, blockedPending.Error!.Code);

        var pendingId = harness.Store.LeaveRequests.Single().Id;
        Assert.True((await harness.RejectLeaveRequest.ExecuteAsync(
            new RejectLeaveRequestCommand(pendingId, "no", "actor"), CancellationToken.None)).IsSuccess);

        Assert.True((await harness.CreateLeaveRequest.ExecuteAsync(
            new CreateLeaveRequestCommand(
                employeeId, employmentId, leaveType.Id, Start, End, 3.0m, null, "actor"),
            CancellationToken.None)).IsSuccess);

        Assert.True((await harness.RecordLeave.ExecuteAsync(
            new RecordLeaveCommand(
                employeeId, employmentId, leaveType.Id,
                Start.AddDays(20), Start.AddDays(22), 3.0m, null, "actor"),
            CancellationToken.None)).IsSuccess);

        var blockedRecord = await harness.CreateLeaveRequest.ExecuteAsync(
            new CreateLeaveRequestCommand(
                employeeId, employmentId, leaveType.Id,
                Start.AddDays(21), Start.AddDays(23), 3.0m, null, "actor"),
            CancellationToken.None);
        Assert.Equal(LeaveValidation.Codes.LeaveRequestOverlap, blockedRecord.Error!.Code);
    }

    [Fact]
    public async Task DepartmentApprove_MovesToHr_NoLeaveRecord()
    {
        var harness = await SeedPendingRequestAsync();
        var requestId = harness.Store.LeaveRequests.Single().Id;

        var approved = await harness.ApproveLeaveRequestDepartment.ExecuteAsync(
            new ApproveLeaveRequestDepartmentCommand(requestId, "dept"),
            CancellationToken.None);

        Assert.True(approved.IsSuccess, approved.Error?.Detail);
        Assert.Equal(LeaveRequestStatus.Pending, approved.Value!.Status);
        Assert.Equal(LeaveRequestApprovalStage.Hr, approved.Value.ApprovalStage);
        Assert.Empty(harness.Store.LeaveRecords);
        Assert.Single(harness.Store.LeaveRequestDecisions);
        Assert.Equal(LeaveRequestDecisionKind.Approved, harness.Store.LeaveRequestDecisions.Single().Decision);
        Assert.Equal(LeaveRequestApprovalStage.Department, harness.Store.LeaveRequestDecisions.Single().Stage);
    }

    [Fact]
    public async Task HrApprove_CreatesOneLeaveRecord_Atomically()
    {
        var harness = await SeedPendingRequestAsync();
        var requestId = harness.Store.LeaveRequests.Single().Id;
        Assert.True((await harness.ApproveLeaveRequestDepartment.ExecuteAsync(
            new ApproveLeaveRequestDepartmentCommand(requestId, "dept"),
            CancellationToken.None)).IsSuccess);

        var approved = await harness.ApproveLeaveRequestHr.ExecuteAsync(
            new ApproveLeaveRequestHrCommand(requestId, 2.5m, "ok", "hr"),
            CancellationToken.None);

        Assert.True(approved.IsSuccess, approved.Error?.Detail);
        Assert.Equal(LeaveRequestStatus.Approved, approved.Value!.Status);
        Assert.Equal(LeaveRequestApprovalStage.Done, approved.Value.ApprovalStage);
        Assert.Single(harness.Store.LeaveRecords);
        var record = harness.Store.LeaveRecords.Single();
        Assert.Equal(requestId, record.SourceLeaveRequestId);
        Assert.Equal(2.5m, record.Amount);
        Assert.Equal(2, harness.Store.LeaveRequestDecisions.Count);
    }

    [Fact]
    public async Task HrApprove_DoubleAttempt_DoesNotDuplicateRecord()
    {
        var harness = await SeedPendingRequestAsync();
        var requestId = harness.Store.LeaveRequests.Single().Id;
        Assert.True((await harness.ApproveLeaveRequestDepartment.ExecuteAsync(
            new ApproveLeaveRequestDepartmentCommand(requestId, "dept"),
            CancellationToken.None)).IsSuccess);

        Assert.True((await harness.ApproveLeaveRequestHr.ExecuteAsync(
            new ApproveLeaveRequestHrCommand(requestId, 3.0m, null, "hr"),
            CancellationToken.None)).IsSuccess);

        var second = await harness.ApproveLeaveRequestHr.ExecuteAsync(
            new ApproveLeaveRequestHrCommand(requestId, 3.0m, null, "hr"),
            CancellationToken.None);

        Assert.False(second.IsSuccess);
        Assert.Equal(LeaveValidation.Codes.LeaveRequestAlreadyFinalized, second.Error!.Code);
        Assert.Single(harness.Store.LeaveRecords);
    }

    [Fact]
    public async Task HrApprove_SaveFailure_RollsBackRequestAndRecord()
    {
        var harness = await SeedPendingRequestAsync();
        var requestId = harness.Store.LeaveRequests.Single().Id;
        Assert.True((await harness.ApproveLeaveRequestDepartment.ExecuteAsync(
            new ApproveLeaveRequestDepartmentCommand(requestId, "dept"),
            CancellationToken.None)).IsSuccess);

        harness.Store.FailSaveChangesAfterCount = 1;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.ApproveLeaveRequestHr.ExecuteAsync(
                new ApproveLeaveRequestHrCommand(requestId, 3.0m, null, "hr"),
                CancellationToken.None));

        var request = harness.Store.LeaveRequests.Single();
        Assert.Equal(LeaveRequestStatus.Pending, request.Status);
        Assert.Equal(LeaveRequestApprovalStage.Hr, request.ApprovalStage);
        Assert.Empty(harness.Store.LeaveRecords);
        Assert.Single(harness.Store.LeaveRequestDecisions);
    }

    [Fact]
    public async Task HrApprove_InactiveLeaveTypeAfterSubmit_StillAllowed()
    {
        var harness = await SeedPendingRequestAsync();
        var requestId = harness.Store.LeaveRequests.Single().Id;
        var leaveType = harness.Store.LeaveTypes.Single();
        leaveType.Deactivate("admin", harness.Clock.UtcNow);
        Assert.True((await harness.ApproveLeaveRequestDepartment.ExecuteAsync(
            new ApproveLeaveRequestDepartmentCommand(requestId, "dept"),
            CancellationToken.None)).IsSuccess);

        var approved = await harness.ApproveLeaveRequestHr.ExecuteAsync(
            new ApproveLeaveRequestHrCommand(requestId, 3.0m, null, "hr"),
            CancellationToken.None);

        Assert.True(approved.IsSuccess, approved.Error?.Detail);
        Assert.Single(harness.Store.LeaveRecords);
    }

    [Fact]
    public async Task ApprovedCancellation_CancelsRecordAndRequest_Atomically()
    {
        var harness = await SeedPendingRequestAsync();
        var leaveType = harness.Store.LeaveTypes.Single();
        var employeeId = harness.Store.Employments.Single().EmployeeId;
        await harness.RecordLeaveEntitlement.ExecuteAsync(
            new RecordLeaveEntitlementCommand(
                employeeId, null, leaveType.Id, new DateOnly(2026, 1, 1), 14.0m,
                LeaveEntitlementSource.Entitlement, null, "actor"),
            CancellationToken.None);

        var requestId = harness.Store.LeaveRequests.Single().Id;
        Assert.True((await harness.ApproveLeaveRequestDepartment.ExecuteAsync(
            new ApproveLeaveRequestDepartmentCommand(requestId, "dept"),
            CancellationToken.None)).IsSuccess);
        Assert.True((await harness.ApproveLeaveRequestHr.ExecuteAsync(
            new ApproveLeaveRequestHrCommand(requestId, 3.0m, null, "hr"),
            CancellationToken.None)).IsSuccess);

        var overviewBefore = await harness.LeaveQuery.ExecuteAsync(employeeId, null, CancellationToken.None);
        Assert.Equal(11.0m, overviewBefore.Value!.Balances.Single().Remaining);

        var cancelled = await harness.CancelApprovedLeaveRequest.ExecuteAsync(
            new CancelApprovedLeaveRequestCommand(requestId, "mistake", "hr"),
            CancellationToken.None);

        Assert.True(cancelled.IsSuccess, cancelled.Error?.Detail);
        Assert.Equal(LeaveRequestStatus.Cancelled, cancelled.Value!.Status);
        Assert.Equal(LeaveRecordStatus.Cancelled, harness.Store.LeaveRecords.Single().Status);

        var overviewAfter = await harness.LeaveQuery.ExecuteAsync(employeeId, null, CancellationToken.None);
        Assert.Equal(14.0m, overviewAfter.Value!.Balances.Single().Remaining);
    }

    [Fact]
    public async Task DirectHrLeaveRecord_NullSource_RemainsValid()
    {
        var harness = new WorkforceHarness();
        var leaveType = harness.SeedLeaveType("annual", "Annual", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, _) = await harness.SeedEmploymentAsync();

        var recorded = await harness.RecordLeave.ExecuteAsync(
            new RecordLeaveCommand(
                employeeId, null, leaveType.Id, Start, End, 3.0m, null, "actor"),
            CancellationToken.None);

        Assert.True(recorded.IsSuccess, recorded.Error?.Detail);
        Assert.Null(harness.Store.LeaveRecords.Single().SourceLeaveRequestId);
    }

    [Fact]
    public async Task Withdraw_Pending_Succeeds_ApprovedBlocked()
    {
        var harness = await SeedPendingRequestAsync();
        var requestId = harness.Store.LeaveRequests.Single().Id;
        Assert.True((await harness.WithdrawLeaveRequest.ExecuteAsync(
            new WithdrawLeaveRequestCommand(requestId, null, "actor"),
            CancellationToken.None)).IsSuccess);
        Assert.Equal(LeaveRequestStatus.Cancelled, harness.Store.LeaveRequests.Single().Status);

        var harness2 = await SeedPendingRequestAsync();
        var approvedId = harness2.Store.LeaveRequests.Single().Id;
        Assert.True((await harness2.ApproveLeaveRequestDepartment.ExecuteAsync(
            new ApproveLeaveRequestDepartmentCommand(approvedId, "dept"),
            CancellationToken.None)).IsSuccess);
        Assert.True((await harness2.ApproveLeaveRequestHr.ExecuteAsync(
            new ApproveLeaveRequestHrCommand(approvedId, 3.0m, null, "hr"),
            CancellationToken.None)).IsSuccess);

        var withdrawApproved = await harness2.WithdrawLeaveRequest.ExecuteAsync(
            new WithdrawLeaveRequestCommand(approvedId, null, "actor"),
            CancellationToken.None);
        Assert.Equal(LeaveValidation.Codes.LeaveRequestAlreadyFinalized, withdrawApproved.Error!.Code);
    }

    private static bool TryCreateRequest(
        DateOnly start,
        DateOnly end,
        decimal amount,
        out LeaveRequest? request,
        out string? errorCode) =>
        LeaveRequest.TryCreate(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            start,
            end,
            amount,
            reason: null,
            "actor",
            Now,
            out request,
            out _,
            out errorCode);

    private static async Task<WorkforceHarness> SeedPendingRequestAsync()
    {
        var harness = new WorkforceHarness();
        var leaveType = harness.SeedLeaveType("annual", "Annual", tracksBalance: true, LeaveTypeSystemKind.Annual);
        var (employeeId, employmentId) = await harness.SeedEmploymentAsync();
        var created = await harness.CreateLeaveRequest.ExecuteAsync(
            new CreateLeaveRequestCommand(
                employeeId, employmentId, leaveType.Id, Start, End, 3.0m, null, "actor"),
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error?.Detail);
        return harness;
    }
}
