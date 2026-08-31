namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// Leave request workflow intent (HR-05B). Distinct from <see cref="LeaveRecord"/> (authoritative fact).
/// AssignmentId is resolved server-side from StartDate; client never supplies it.
/// No reopen; no direct client status mutation — transitions only via domain methods.
/// </summary>
public sealed class LeaveRequest
{
    public const int ReasonMaxLength = 500;
    public const int UserIdMaxLength = 450;

    private LeaveRequest()
    {
        CreatedByUserId = string.Empty;
    }

    private LeaveRequest(
        Guid id,
        Guid employmentId,
        Guid assignmentId,
        Guid leaveTypeId,
        DateOnly startDate,
        DateOnly endDate,
        decimal requestedAmount,
        string? reason,
        string actorUserId,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        EmploymentId = employmentId;
        AssignmentId = assignmentId;
        LeaveTypeId = leaveTypeId;
        StartDate = startDate;
        EndDate = endDate;
        RequestedAmount = requestedAmount;
        Reason = reason;
        Status = LeaveRequestStatus.Pending;
        ApprovalStage = LeaveRequestApprovalStage.Department;
        CreatedByUserId = actorUserId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid EmploymentId { get; private set; }
    public Guid AssignmentId { get; private set; }
    public Guid LeaveTypeId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public decimal RequestedAmount { get; private set; }
    public LeaveRequestStatus Status { get; private set; }
    public LeaveRequestApprovalStage ApprovalStage { get; private set; }
    public string? Reason { get; private set; }
    public string CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public bool IsPending => Status == LeaveRequestStatus.Pending;
    public bool IsFinalized => Status is LeaveRequestStatus.Approved
        or LeaveRequestStatus.Rejected
        or LeaveRequestStatus.Cancelled;

    public static bool TryCreate(
        Guid id,
        Guid employmentId,
        Guid assignmentId,
        Guid leaveTypeId,
        DateOnly startDate,
        DateOnly endDate,
        decimal requestedAmount,
        string? reason,
        string actorUserId,
        DateTimeOffset createdAtUtc,
        out LeaveRequest? request,
        out string? field,
        out string? errorCode)
    {
        request = null;
        field = null;
        errorCode = null;

        if (startDate > endDate)
        {
            field = LeaveValidation.Fields.EndDate;
            errorCode = LeaveValidation.Codes.LeaveInvalidDateRange;
            return false;
        }

        if (!LeaveAmount.IsValidPositive(requestedAmount))
        {
            field = LeaveValidation.Fields.RequestedAmount;
            errorCode = LeaveValidation.Codes.LeaveRequestInvalidAmount;
            return false;
        }

        var trimmedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (trimmedReason is { Length: > ReasonMaxLength })
        {
            field = LeaveValidation.Fields.Reason;
            errorCode = LeaveValidation.Codes.LeaveRequestReasonTooLong;
            return false;
        }

        request = new LeaveRequest(
            id,
            employmentId,
            assignmentId,
            leaveTypeId,
            startDate,
            endDate,
            requestedAmount,
            trimmedReason,
            actorUserId,
            createdAtUtc);
        return true;
    }

    public bool TryApproveDepartment(
        string actorUserId,
        DateTimeOffset utcNow,
        out LeaveRequestDecision? decision,
        out string? errorCode)
    {
        decision = null;
        errorCode = null;

        if (!IsPending)
        {
            errorCode = IsFinalized
                ? LeaveValidation.Codes.LeaveRequestAlreadyFinalized
                : LeaveValidation.Codes.LeaveRequestNotPending;
            return false;
        }

        if (ApprovalStage != LeaveRequestApprovalStage.Department)
        {
            errorCode = LeaveValidation.Codes.LeaveRequestInvalidApprovalStage;
            return false;
        }

        ApprovalStage = LeaveRequestApprovalStage.Hr;
        UpdatedAtUtc = utcNow;
        decision = LeaveRequestDecision.Create(
            Guid.CreateVersion7(),
            Id,
            LeaveRequestApprovalStage.Department,
            LeaveRequestDecisionKind.Approved,
            actorUserId,
            utcNow,
            note: null);
        return true;
    }

    public bool TryApproveHr(
        string actorUserId,
        DateTimeOffset utcNow,
        string? note,
        out LeaveRequestDecision? decision,
        out string? errorCode)
    {
        decision = null;
        errorCode = null;

        if (!IsPending)
        {
            errorCode = IsFinalized
                ? LeaveValidation.Codes.LeaveRequestAlreadyFinalized
                : LeaveValidation.Codes.LeaveRequestNotPending;
            return false;
        }

        if (ApprovalStage != LeaveRequestApprovalStage.Hr)
        {
            errorCode = LeaveValidation.Codes.LeaveRequestInvalidApprovalStage;
            return false;
        }

        Status = LeaveRequestStatus.Approved;
        ApprovalStage = LeaveRequestApprovalStage.Done;
        UpdatedAtUtc = utcNow;
        decision = LeaveRequestDecision.Create(
            Guid.CreateVersion7(),
            Id,
            LeaveRequestApprovalStage.Hr,
            LeaveRequestDecisionKind.Approved,
            actorUserId,
            utcNow,
            note);
        return true;
    }

    public bool TryReject(
        string actorUserId,
        DateTimeOffset utcNow,
        string? note,
        out LeaveRequestDecision? decision,
        out string? errorCode)
    {
        decision = null;
        errorCode = null;

        if (!IsPending)
        {
            errorCode = IsFinalized
                ? LeaveValidation.Codes.LeaveRequestAlreadyFinalized
                : LeaveValidation.Codes.LeaveRequestNotPending;
            return false;
        }

        if (ApprovalStage is not (LeaveRequestApprovalStage.Department or LeaveRequestApprovalStage.Hr))
        {
            errorCode = LeaveValidation.Codes.LeaveRequestInvalidApprovalStage;
            return false;
        }

        var stage = ApprovalStage;
        Status = LeaveRequestStatus.Rejected;
        ApprovalStage = LeaveRequestApprovalStage.Done;
        UpdatedAtUtc = utcNow;
        decision = LeaveRequestDecision.Create(
            Guid.CreateVersion7(),
            Id,
            stage,
            LeaveRequestDecisionKind.Rejected,
            actorUserId,
            utcNow,
            note);
        return true;
    }

    public bool TryWithdraw(
        string actorUserId,
        DateTimeOffset utcNow,
        string? note,
        out LeaveRequestDecision? decision,
        out string? errorCode)
    {
        decision = null;
        errorCode = null;

        if (!IsPending)
        {
            errorCode = IsFinalized
                ? LeaveValidation.Codes.LeaveRequestAlreadyFinalized
                : LeaveValidation.Codes.LeaveRequestNotPending;
            return false;
        }

        var stage = ApprovalStage;
        Status = LeaveRequestStatus.Cancelled;
        ApprovalStage = LeaveRequestApprovalStage.Done;
        UpdatedAtUtc = utcNow;
        decision = LeaveRequestDecision.Create(
            Guid.CreateVersion7(),
            Id,
            stage,
            LeaveRequestDecisionKind.Cancelled,
            actorUserId,
            utcNow,
            note);
        return true;
    }

    public bool TryCancelApproved(
        string actorUserId,
        DateTimeOffset utcNow,
        string? note,
        out LeaveRequestDecision? decision,
        out string? errorCode)
    {
        decision = null;
        errorCode = null;

        if (Status != LeaveRequestStatus.Approved || ApprovalStage != LeaveRequestApprovalStage.Done)
        {
            errorCode = LeaveValidation.Codes.LeaveRequestNotPending;
            return false;
        }

        Status = LeaveRequestStatus.Cancelled;
        ApprovalStage = LeaveRequestApprovalStage.Done;
        UpdatedAtUtc = utcNow;
        decision = LeaveRequestDecision.Create(
            Guid.CreateVersion7(),
            Id,
            LeaveRequestApprovalStage.Done,
            LeaveRequestDecisionKind.Cancelled,
            actorUserId,
            utcNow,
            note);
        return true;
    }

    /// <summary>
    /// Restores workflow fields for in-memory transaction rollback in tests.
    /// </summary>
    internal void RestoreWorkflowState(
        LeaveRequestStatus status,
        LeaveRequestApprovalStage approvalStage,
        DateTimeOffset updatedAtUtc)
    {
        Status = status;
        ApprovalStage = approvalStage;
        UpdatedAtUtc = updatedAtUtc;
    }
}
