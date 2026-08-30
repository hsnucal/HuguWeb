namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// Append-only audit of meaningful schedule mutations. History survives Clear → Unscheduled.
/// Nullable Kind values represent Unscheduled (no authoritative row).
/// </summary>
public sealed class ScheduleEntryChange
{
    public const int UserIdMaxLength = 450;

    private ScheduleEntryChange()
    {
        ChangedByUserId = string.Empty;
    }

    private ScheduleEntryChange(
        Guid id,
        Guid employmentId,
        DateOnly scheduleDate,
        Guid? scheduleEntryId,
        ScheduleEntryKind? previousKind,
        Guid? previousShiftDefinitionId,
        ScheduleEntryKind? newKind,
        Guid? newShiftDefinitionId,
        string changedByUserId,
        DateTimeOffset changedAtUtc)
    {
        Id = id;
        EmploymentId = employmentId;
        ScheduleDate = scheduleDate;
        ScheduleEntryId = scheduleEntryId;
        PreviousKind = previousKind;
        PreviousShiftDefinitionId = previousShiftDefinitionId;
        NewKind = newKind;
        NewShiftDefinitionId = newShiftDefinitionId;
        ChangedByUserId = changedByUserId;
        ChangedAtUtc = changedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid EmploymentId { get; private set; }
    public DateOnly ScheduleDate { get; private set; }
    public Guid? ScheduleEntryId { get; private set; }
    public ScheduleEntryKind? PreviousKind { get; private set; }
    public Guid? PreviousShiftDefinitionId { get; private set; }
    public ScheduleEntryKind? NewKind { get; private set; }
    public Guid? NewShiftDefinitionId { get; private set; }
    public string ChangedByUserId { get; private set; }
    public DateTimeOffset ChangedAtUtc { get; private set; }

    public static ScheduleEntryChange Record(
        Guid id,
        Guid employmentId,
        DateOnly scheduleDate,
        Guid? scheduleEntryId,
        ScheduleEntryKind? previousKind,
        Guid? previousShiftDefinitionId,
        ScheduleEntryKind? newKind,
        Guid? newShiftDefinitionId,
        string changedByUserId,
        DateTimeOffset changedAtUtc) =>
        new(
            id,
            employmentId,
            scheduleDate,
            scheduleEntryId,
            previousKind,
            previousShiftDefinitionId,
            newKind,
            newShiftDefinitionId,
            changedByUserId,
            changedAtUtc);

    public static ScheduleEntryChange FromMutation(
        Guid id,
        ScheduleEntry? previous,
        ScheduleEntry? next,
        Guid employmentId,
        DateOnly scheduleDate,
        string actorUserId,
        DateTimeOffset utcNow) =>
        Record(
            id,
            employmentId,
            scheduleDate,
            next?.Id ?? previous?.Id,
            previous?.Kind,
            previous?.ShiftDefinitionId,
            next?.Kind,
            next?.ShiftDefinitionId,
            actorUserId,
            utcNow);
}
