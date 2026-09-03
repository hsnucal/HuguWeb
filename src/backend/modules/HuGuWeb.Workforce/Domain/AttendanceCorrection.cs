namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// Current-state manual Puantaj override for one Employment × Property-local DateOnly.
/// At most one current row per employment/date. History lives on AttendanceCorrectionChange.
/// Clear removes this row; it does not mutate ScheduleEntry or LeaveRecord.
/// </summary>
public sealed class AttendanceCorrection
{
    public const int ReasonMaxLength = 500;
    public const int UserIdMaxLength = 450;

    private AttendanceCorrection()
    {
        Reason = string.Empty;
        CreatedByUserId = string.Empty;
        UpdatedByUserId = string.Empty;
    }

    private AttendanceCorrection(
        Guid id,
        Guid organizationId,
        Guid propertyId,
        Guid employmentId,
        Guid assignmentId,
        DateOnly localDate,
        AttendanceCorrectionKind kind,
        string reason,
        string actorUserId,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        PropertyId = propertyId;
        EmploymentId = employmentId;
        AssignmentId = assignmentId;
        LocalDate = localDate;
        Kind = kind;
        Reason = reason;
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = actorUserId;
        UpdatedAtUtc = createdAtUtc;
        UpdatedByUserId = actorUserId;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid EmploymentId { get; private set; }
    public Guid AssignmentId { get; private set; }
    public DateOnly LocalDate { get; private set; }
    public AttendanceCorrectionKind Kind { get; private set; }
    public string Reason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public string CreatedByUserId { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public string UpdatedByUserId { get; private set; }

    public AttendanceAcceptedKind AcceptedKind => ToAcceptedKind(Kind);

    public static bool TryCreate(
        Guid id,
        Guid organizationId,
        Guid propertyId,
        Guid employmentId,
        Guid assignmentId,
        DateOnly localDate,
        AttendanceCorrectionKind kind,
        string? reason,
        string actorUserId,
        DateTimeOffset createdAtUtc,
        out AttendanceCorrection? correction,
        out string? field,
        out string? errorCode)
    {
        correction = null;
        if (!IsSupportedKind(kind))
        {
            field = AttendanceValidation.Fields.Kind;
            errorCode = AttendanceValidation.Codes.AttendanceCorrectionKindInvalid;
            return false;
        }

        if (!TryNormalizeReason(reason, out var normalized, out field, out errorCode))
        {
            return false;
        }

        correction = new AttendanceCorrection(
            id,
            organizationId,
            propertyId,
            employmentId,
            assignmentId,
            localDate,
            kind,
            normalized,
            actorUserId,
            createdAtUtc);
        return true;
    }

    public bool TryReplace(
        Guid assignmentId,
        AttendanceCorrectionKind kind,
        string? reason,
        string actorUserId,
        DateTimeOffset utcNow,
        out string? field,
        out string? errorCode)
    {
        if (!IsSupportedKind(kind))
        {
            field = AttendanceValidation.Fields.Kind;
            errorCode = AttendanceValidation.Codes.AttendanceCorrectionKindInvalid;
            return false;
        }

        if (!TryNormalizeReason(reason, out var normalized, out field, out errorCode))
        {
            return false;
        }

        AssignmentId = assignmentId;
        Kind = kind;
        Reason = normalized;
        UpdatedByUserId = actorUserId;
        UpdatedAtUtc = utcNow;
        return true;
    }

    public bool HasSameCurrentValues(Guid assignmentId, AttendanceCorrectionKind kind, string? reason)
    {
        if (!TryNormalizeReason(reason, out var normalized, out _, out _))
        {
            return false;
        }

        return AssignmentId == assignmentId
            && Kind == kind
            && string.Equals(Reason, normalized, StringComparison.Ordinal);
    }

    public static bool IsSupportedKind(AttendanceCorrectionKind kind) =>
        kind is AttendanceCorrectionKind.Worked
            or AttendanceCorrectionKind.Leave
            or AttendanceCorrectionKind.RestDay
            or AttendanceCorrectionKind.Absent;

    public static bool TryParseKind(string? value, out AttendanceCorrectionKind kind)
    {
        kind = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        if (int.TryParse(trimmed, out _))
        {
            return false;
        }

        return Enum.TryParse(trimmed, ignoreCase: true, out kind)
            && IsSupportedKind(kind)
            && string.Equals(Enum.GetName(kind), trimmed, StringComparison.OrdinalIgnoreCase);
    }

    public static AttendanceAcceptedKind ToAcceptedKind(AttendanceCorrectionKind kind) =>
        kind switch
        {
            AttendanceCorrectionKind.Worked => AttendanceAcceptedKind.Worked,
            AttendanceCorrectionKind.Leave => AttendanceAcceptedKind.Leave,
            AttendanceCorrectionKind.RestDay => AttendanceAcceptedKind.RestDay,
            AttendanceCorrectionKind.Absent => AttendanceAcceptedKind.Absent,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

    public static bool TryNormalizeReason(string? reason, out string normalized, out string? field, out string? errorCode)
    {
        normalized = string.Empty;
        field = AttendanceValidation.Fields.Reason;
        if (string.IsNullOrWhiteSpace(reason))
        {
            errorCode = AttendanceValidation.Codes.AttendanceCorrectionReasonRequired;
            return false;
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > ReasonMaxLength)
        {
            errorCode = AttendanceValidation.Codes.AttendanceCorrectionReasonTooLong;
            return false;
        }

        normalized = trimmed;
        field = null;
        errorCode = null;
        return true;
    }
}
