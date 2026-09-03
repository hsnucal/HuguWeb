namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// Append-only audit of set/clear attendance corrections. Survives deletion of the current row.
/// </summary>
public sealed class AttendanceCorrectionChange
{
    public const int UserIdMaxLength = 450;
    public const int ReasonMaxLength = AttendanceCorrection.ReasonMaxLength;

    private AttendanceCorrectionChange()
    {
        ChangedByUserId = string.Empty;
    }

    private AttendanceCorrectionChange(
        Guid id,
        Guid employmentId,
        DateOnly localDate,
        Guid? correctionId,
        AttendanceCorrectionKind? previousKind,
        string? previousReason,
        AttendanceCorrectionKind? newKind,
        string? newReason,
        AttendanceChangeType changeType,
        string changedByUserId,
        DateTimeOffset changedAtUtc)
    {
        Id = id;
        EmploymentId = employmentId;
        LocalDate = localDate;
        CorrectionId = correctionId;
        PreviousKind = previousKind;
        PreviousReason = previousReason;
        NewKind = newKind;
        NewReason = newReason;
        ChangeType = changeType;
        ChangedByUserId = changedByUserId;
        ChangedAtUtc = changedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid EmploymentId { get; private set; }
    public DateOnly LocalDate { get; private set; }
    public Guid? CorrectionId { get; private set; }
    public AttendanceCorrectionKind? PreviousKind { get; private set; }
    public string? PreviousReason { get; private set; }
    public AttendanceCorrectionKind? NewKind { get; private set; }
    public string? NewReason { get; private set; }
    public AttendanceChangeType ChangeType { get; private set; }
    public string ChangedByUserId { get; private set; }
    public DateTimeOffset ChangedAtUtc { get; private set; }

    public static AttendanceCorrectionChange RecordSet(
        Guid id,
        Guid employmentId,
        DateOnly localDate,
        Guid correctionId,
        AttendanceCorrectionKind? previousKind,
        string? previousReason,
        AttendanceCorrectionKind newKind,
        string newReason,
        string actorUserId,
        DateTimeOffset utcNow) =>
        new(
            id,
            employmentId,
            localDate,
            correctionId,
            previousKind,
            previousReason,
            newKind,
            newReason,
            AttendanceChangeType.Set,
            actorUserId,
            utcNow);

    public static AttendanceCorrectionChange RecordClear(
        Guid id,
        Guid employmentId,
        DateOnly localDate,
        Guid? correctionId,
        AttendanceCorrectionKind previousKind,
        string previousReason,
        string actorUserId,
        DateTimeOffset utcNow) =>
        new(
            id,
            employmentId,
            localDate,
            correctionId,
            previousKind,
            previousReason,
            newKind: null,
            newReason: null,
            AttendanceChangeType.Clear,
            actorUserId,
            utcNow);
}
