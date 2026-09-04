namespace HuGuWeb.Workforce.Domain;

public sealed class PersonnelMovement
{
    public const int ReasonMaxLength = 500;
    public const int NoteMaxLength = 500;
    public const int UserIdMaxLength = 450;

    private PersonnelMovement()
    {
        Reason = string.Empty;
        CreatedByUserId = string.Empty;
    }

    private PersonnelMovement(
        Guid id,
        Guid organizationId,
        Guid employmentId,
        PersonnelMovementType movementType,
        DateOnly effectiveDate,
        Guid? previousAssignmentId,
        Guid? newAssignmentId,
        Guid? previousReportingLineId,
        Guid? newReportingLineId,
        string reason,
        string? note,
        string createdByUserId,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        EmploymentId = employmentId;
        MovementType = movementType;
        EffectiveDate = effectiveDate;
        PreviousAssignmentId = previousAssignmentId;
        NewAssignmentId = newAssignmentId;
        PreviousReportingLineId = previousReportingLineId;
        NewReportingLineId = newReportingLineId;
        Reason = reason;
        Note = note;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid EmploymentId { get; private set; }
    public PersonnelMovementType MovementType { get; private set; }
    public DateOnly EffectiveDate { get; private set; }
    public Guid? PreviousAssignmentId { get; private set; }
    public Guid? NewAssignmentId { get; private set; }
    public Guid? PreviousReportingLineId { get; private set; }
    public Guid? NewReportingLineId { get; private set; }
    public string Reason { get; private set; }
    public string? Note { get; private set; }
    public string CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public string? CancelledByUserId { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }

    public bool IsCancelled => CancelledAtUtc is not null;

    public static bool TryCreate(
        Guid id,
        Guid organizationId,
        Guid employmentId,
        PersonnelMovementType movementType,
        DateOnly effectiveDate,
        Guid? previousAssignmentId,
        Guid? newAssignmentId,
        Guid? previousReportingLineId,
        Guid? newReportingLineId,
        string reason,
        string? note,
        string createdByUserId,
        DateTimeOffset createdAtUtc,
        out PersonnelMovement? movement,
        out string? field,
        out string? errorCode)
    {
        movement = null;
        field = null;
        errorCode = null;

        var trimmedReason = reason?.Trim() ?? string.Empty;
        if (trimmedReason.Length == 0)
        {
            field = MovementValidation.Fields.Reason;
            errorCode = MovementValidation.Codes.ReasonRequired;
            return false;
        }

        if (trimmedReason.Length > ReasonMaxLength)
        {
            field = MovementValidation.Fields.Reason;
            errorCode = MovementValidation.Codes.ReasonTooLong;
            return false;
        }

        string? trimmedNote = null;
        if (!string.IsNullOrWhiteSpace(note))
        {
            trimmedNote = note.Trim();
            if (trimmedNote.Length > NoteMaxLength)
            {
                field = MovementValidation.Fields.Note;
                errorCode = MovementValidation.Codes.NoteTooLong;
                return false;
            }
        }

        var actor = (createdByUserId ?? string.Empty).Trim();
        if (actor.Length > UserIdMaxLength)
        {
            actor = actor[..UserIdMaxLength];
        }

        movement = new PersonnelMovement(
            id,
            organizationId,
            employmentId,
            movementType,
            effectiveDate,
            previousAssignmentId,
            newAssignmentId,
            previousReportingLineId,
            newReportingLineId,
            trimmedReason,
            trimmedNote,
            actor,
            createdAtUtc);
        return true;
    }

    public bool TryCancel(
        string reason,
        string actorUserId,
        DateTimeOffset utcNow,
        out string? field,
        out string? errorCode)
    {
        field = null;
        errorCode = null;

        if (IsCancelled)
        {
            errorCode = MovementValidation.Codes.AlreadyCancelled;
            return false;
        }

        var trimmedReason = reason?.Trim() ?? string.Empty;
        if (trimmedReason.Length == 0)
        {
            field = MovementValidation.Fields.CancellationReason;
            errorCode = MovementValidation.Codes.CancellationReasonRequired;
            return false;
        }

        if (trimmedReason.Length > ReasonMaxLength)
        {
            field = MovementValidation.Fields.CancellationReason;
            errorCode = MovementValidation.Codes.CancellationReasonTooLong;
            return false;
        }

        var actor = (actorUserId ?? string.Empty).Trim();
        if (actor.Length > UserIdMaxLength)
        {
            actor = actor[..UserIdMaxLength];
        }

        CancelledByUserId = actor;
        CancelledAtUtc = utcNow;
        CancellationReason = trimmedReason;
        return true;
    }

    internal void DetachNeverEffectiveSuccessor()
    {
        NewAssignmentId = null;
        NewReportingLineId = null;
    }

    internal void RestoreCancellationState(
        string? cancelledByUserId,
        DateTimeOffset? cancelledAtUtc,
        string? cancellationReason)
    {
        CancelledByUserId = cancelledByUserId;
        CancelledAtUtc = cancelledAtUtc;
        CancellationReason = cancellationReason;
    }

    internal void RestoreSuccessorIds(Guid? newAssignmentId, Guid? newReportingLineId)
    {
        NewAssignmentId = newAssignmentId;
        NewReportingLineId = newReportingLineId;
    }

    public PersonnelMovementLifecycle Lifecycle(DateOnly businessToday)
    {
        if (IsCancelled)
        {
            return PersonnelMovementLifecycle.Cancelled;
        }

        return EffectiveDate <= businessToday
            ? PersonnelMovementLifecycle.Effective
            : PersonnelMovementLifecycle.Scheduled;
    }
}
