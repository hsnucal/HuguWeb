namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// Authoritative leave fact. Amount (in days, half-day quantum) is authoritative; dates are stored
/// for reference. Direct HR entry (HR-05A) keeps <see cref="SourceLeaveRequestId"/> null.
/// Request-approved records set a unique SourceLeaveRequestId (HR-05B). Cancellation retains the row.
/// </summary>
public sealed class LeaveRecord
{
    public const int NoteMaxLength = 500;
    public const int CancellationReasonMaxLength = 500;
    public const int UserIdMaxLength = 450;

    private LeaveRecord()
    {
        CreatedByUserId = string.Empty;
    }

    private LeaveRecord(
        Guid id,
        Guid employmentId,
        Guid leaveTypeId,
        DateOnly startDate,
        DateOnly endDate,
        decimal amount,
        string? note,
        string actorUserId,
        DateTimeOffset createdAtUtc,
        Guid? sourceLeaveRequestId)
    {
        Id = id;
        EmploymentId = employmentId;
        LeaveTypeId = leaveTypeId;
        StartDate = startDate;
        EndDate = endDate;
        Amount = amount;
        Note = note;
        Status = LeaveRecordStatus.Recorded;
        CreatedByUserId = actorUserId;
        CreatedAtUtc = createdAtUtc;
        SourceLeaveRequestId = sourceLeaveRequestId;
    }

    public Guid Id { get; private set; }
    public Guid EmploymentId { get; private set; }
    public Guid LeaveTypeId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public decimal Amount { get; private set; }
    public LeaveRecordStatus Status { get; private set; }
    public string? Note { get; private set; }
    public string CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public string? CancelledByUserId { get; private set; }
    public string? CancellationReason { get; private set; }
    public Guid? SourceLeaveRequestId { get; private set; }

    public bool IsCancelled => Status == LeaveRecordStatus.Cancelled;

    public static bool TryCreate(
        Guid id,
        Guid employmentId,
        Guid leaveTypeId,
        DateOnly startDate,
        DateOnly endDate,
        decimal amount,
        string? note,
        string actorUserId,
        DateTimeOffset createdAtUtc,
        out LeaveRecord? record,
        out string? field,
        out string? errorCode,
        Guid? sourceLeaveRequestId = null)
    {
        record = null;
        field = null;
        errorCode = null;

        if (startDate > endDate)
        {
            field = LeaveValidation.Fields.EndDate;
            errorCode = LeaveValidation.Codes.LeaveInvalidDateRange;
            return false;
        }

        if (!LeaveAmount.IsValidPositive(amount))
        {
            field = LeaveValidation.Fields.Amount;
            errorCode = LeaveValidation.Codes.LeaveInvalidAmount;
            return false;
        }

        var trimmedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (trimmedNote is { Length: > NoteMaxLength })
        {
            field = LeaveValidation.Fields.Note;
            errorCode = LeaveValidation.Codes.LeaveNoteTooLong;
            return false;
        }

        record = new LeaveRecord(
            id,
            employmentId,
            leaveTypeId,
            startDate,
            endDate,
            amount,
            trimmedNote,
            actorUserId,
            createdAtUtc,
            sourceLeaveRequestId);
        return true;
    }

    /// <summary>
    /// Restores cancellation fields for in-memory transaction rollback in tests.
    /// </summary>
    internal void RestoreCancellationState(
        LeaveRecordStatus status,
        DateTimeOffset? cancelledAtUtc,
        string? cancelledByUserId,
        string? cancellationReason)
    {
        Status = status;
        CancelledAtUtc = cancelledAtUtc;
        CancelledByUserId = cancelledByUserId;
        CancellationReason = cancellationReason;
    }

    public bool TryCancel(string? reason, string actorUserId, DateTimeOffset utcNow, out string? field, out string? errorCode)
    {
        field = null;
        errorCode = null;

        if (Status == LeaveRecordStatus.Cancelled)
        {
            errorCode = LeaveValidation.Codes.LeaveAlreadyCancelled;
            return false;
        }

        var trimmed = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (trimmed is null)
        {
            field = LeaveValidation.Fields.CancellationReason;
            errorCode = LeaveValidation.Codes.LeaveCancellationReasonRequired;
            return false;
        }

        if (trimmed.Length > CancellationReasonMaxLength)
        {
            trimmed = trimmed[..CancellationReasonMaxLength];
        }

        Status = LeaveRecordStatus.Cancelled;
        CancellationReason = trimmed;
        CancelledByUserId = actorUserId;
        CancelledAtUtc = utcNow;
        return true;
    }
}
