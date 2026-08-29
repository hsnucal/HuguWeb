namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// Immutable entitlement movement (grant, carry-over, or manual adjustment). Never a snapshot of
/// remaining balance. Corrections are made by adding a new <see cref="LeaveEntitlementSource.ManualAdjustment"/>.
/// </summary>
public sealed class LeaveEntitlement
{
    public const int NoteMaxLength = 500;
    public const int UserIdMaxLength = 450;

    private LeaveEntitlement()
    {
        CreatedByUserId = string.Empty;
    }

    private LeaveEntitlement(
        Guid id,
        Guid employmentId,
        Guid leaveTypeId,
        DateOnly effectiveDate,
        decimal amount,
        LeaveEntitlementSource source,
        string? note,
        string actorUserId,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        EmploymentId = employmentId;
        LeaveTypeId = leaveTypeId;
        EffectiveDate = effectiveDate;
        Amount = amount;
        Source = source;
        Note = note;
        CreatedByUserId = actorUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid EmploymentId { get; private set; }
    public Guid LeaveTypeId { get; private set; }
    public DateOnly EffectiveDate { get; private set; }
    public decimal Amount { get; private set; }
    public LeaveEntitlementSource Source { get; private set; }
    public string? Note { get; private set; }
    public string CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static bool TryCreate(
        Guid id,
        Guid employmentId,
        Guid leaveTypeId,
        DateOnly effectiveDate,
        decimal amount,
        LeaveEntitlementSource source,
        string? note,
        string actorUserId,
        DateTimeOffset createdAtUtc,
        out LeaveEntitlement? entitlement,
        out string? field,
        out string? errorCode)
    {
        entitlement = null;
        field = null;
        errorCode = null;

        if (!Enum.IsDefined(source))
        {
            field = LeaveValidation.Fields.Source;
            errorCode = LeaveValidation.Codes.LeaveEntitlementInvalidSource;
            return false;
        }

        var amountValid = source == LeaveEntitlementSource.ManualAdjustment
            ? LeaveAmount.IsValidNonZero(amount)
            : LeaveAmount.IsValidPositive(amount);
        if (!amountValid)
        {
            field = LeaveValidation.Fields.Amount;
            errorCode = LeaveValidation.Codes.LeaveEntitlementInvalidAmount;
            return false;
        }

        var trimmedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (source == LeaveEntitlementSource.ManualAdjustment && trimmedNote is null)
        {
            field = LeaveValidation.Fields.Note;
            errorCode = LeaveValidation.Codes.LeaveEntitlementNoteRequired;
            return false;
        }

        if (trimmedNote is { Length: > NoteMaxLength })
        {
            field = LeaveValidation.Fields.Note;
            errorCode = LeaveValidation.Codes.LeaveNoteTooLong;
            return false;
        }

        entitlement = new LeaveEntitlement(
            id,
            employmentId,
            leaveTypeId,
            effectiveDate,
            amount,
            source,
            trimmedNote,
            actorUserId,
            createdAtUtc);
        return true;
    }
}
