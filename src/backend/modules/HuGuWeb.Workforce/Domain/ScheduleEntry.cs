namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// Daily authoritative schedule presence for an Employment. Unscheduled = no row.
/// AssignmentId pins historical workplace context at write time.
/// </summary>
public sealed class ScheduleEntry
{
    public const int NoteMaxLength = 500;
    public const int UserIdMaxLength = 450;

    private ScheduleEntry()
    {
        CreatedByUserId = string.Empty;
        UpdatedByUserId = string.Empty;
    }

    private ScheduleEntry(
        Guid id,
        Guid employmentId,
        Guid assignmentId,
        DateOnly scheduleDate,
        ScheduleEntryKind kind,
        Guid? shiftDefinitionId,
        string? note,
        string actorUserId,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        EmploymentId = employmentId;
        AssignmentId = assignmentId;
        ScheduleDate = scheduleDate;
        Kind = kind;
        ShiftDefinitionId = shiftDefinitionId;
        Note = note;
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = actorUserId;
        UpdatedAtUtc = createdAtUtc;
        UpdatedByUserId = actorUserId;
    }

    public Guid Id { get; private set; }
    public Guid EmploymentId { get; private set; }
    public Guid AssignmentId { get; private set; }
    public DateOnly ScheduleDate { get; private set; }
    public ScheduleEntryKind Kind { get; private set; }
    public Guid? ShiftDefinitionId { get; private set; }
    public string? Note { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public string CreatedByUserId { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public string UpdatedByUserId { get; private set; }

    public static bool TryCreateShift(
        Guid id,
        Guid employmentId,
        Guid assignmentId,
        DateOnly scheduleDate,
        Guid shiftDefinitionId,
        string? note,
        string actorUserId,
        DateTimeOffset createdAtUtc,
        out ScheduleEntry? entry,
        out string? field,
        out string? errorCode)
    {
        entry = null;
        if (!TryNormalizeNote(note, out var normalizedNote, out field, out errorCode))
        {
            return false;
        }

        if (shiftDefinitionId == Guid.Empty)
        {
            field = ScheduleValidation.Fields.ShiftDefinitionId;
            errorCode = ScheduleValidation.Codes.ScheduleShiftDefinitionRequired;
            return false;
        }

        entry = new ScheduleEntry(
            id,
            employmentId,
            assignmentId,
            scheduleDate,
            ScheduleEntryKind.Shift,
            shiftDefinitionId,
            normalizedNote,
            actorUserId,
            createdAtUtc);
        field = null;
        errorCode = null;
        return true;
    }

    public static bool TryCreateRestDay(
        Guid id,
        Guid employmentId,
        Guid assignmentId,
        DateOnly scheduleDate,
        string? note,
        string actorUserId,
        DateTimeOffset createdAtUtc,
        out ScheduleEntry? entry,
        out string? field,
        out string? errorCode)
    {
        entry = null;
        if (!TryNormalizeNote(note, out var normalizedNote, out field, out errorCode))
        {
            return false;
        }

        entry = new ScheduleEntry(
            id,
            employmentId,
            assignmentId,
            scheduleDate,
            ScheduleEntryKind.RestDay,
            shiftDefinitionId: null,
            normalizedNote,
            actorUserId,
            createdAtUtc);
        field = null;
        errorCode = null;
        return true;
    }

    public bool TryAssignShift(
        Guid assignmentId,
        Guid shiftDefinitionId,
        string? note,
        string actorUserId,
        DateTimeOffset utcNow,
        out string? field,
        out string? errorCode)
    {
        if (shiftDefinitionId == Guid.Empty)
        {
            field = ScheduleValidation.Fields.ShiftDefinitionId;
            errorCode = ScheduleValidation.Codes.ScheduleShiftDefinitionRequired;
            return false;
        }

        if (!TryNormalizeNote(note, out var normalizedNote, out field, out errorCode))
        {
            return false;
        }

        AssignmentId = assignmentId;
        Kind = ScheduleEntryKind.Shift;
        ShiftDefinitionId = shiftDefinitionId;
        Note = normalizedNote;
        Touch(actorUserId, utcNow);
        return true;
    }

    public bool TryMarkRestDay(
        Guid assignmentId,
        string? note,
        string actorUserId,
        DateTimeOffset utcNow,
        out string? field,
        out string? errorCode)
    {
        if (!TryNormalizeNote(note, out var normalizedNote, out field, out errorCode))
        {
            return false;
        }

        AssignmentId = assignmentId;
        Kind = ScheduleEntryKind.RestDay;
        ShiftDefinitionId = null;
        Note = normalizedNote;
        Touch(actorUserId, utcNow);
        return true;
    }

    private void Touch(string actorUserId, DateTimeOffset utcNow)
    {
        UpdatedByUserId = actorUserId;
        UpdatedAtUtc = utcNow;
    }

    public static bool TryNormalizeNote(string? note, out string? normalized, out string? field, out string? errorCode)
    {
        field = null;
        errorCode = null;
        if (string.IsNullOrWhiteSpace(note))
        {
            normalized = null;
            return true;
        }

        var trimmed = note.Trim();
        if (trimmed.Length > NoteMaxLength)
        {
            normalized = null;
            field = ScheduleValidation.Fields.Note;
            errorCode = ScheduleValidation.Codes.ScheduleNoteTooLong;
            return false;
        }

        normalized = trimmed;
        return true;
    }

    public static bool IsWithinEmploymentPeriod(Employment employment, DateOnly scheduleDate) =>
        scheduleDate >= employment.StartDate
        && (employment.EndDate is null || scheduleDate <= employment.EndDate);
}
