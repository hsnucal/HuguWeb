using System.Globalization;

namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// Property-scoped reusable shift catalogue entry. Planned times are local wall-clock;
/// Property.TimeZoneId is the timezone source (not copied onto this entity).
/// </summary>
public sealed class ShiftDefinition
{
    public const int CodeMaxLength = 32;
    public const int NameMaxLength = 200;
    public const int UserIdMaxLength = 450;

    private ShiftDefinition()
    {
        Code = string.Empty;
        Name = string.Empty;
        CreatedByUserId = string.Empty;
        UpdatedByUserId = string.Empty;
    }

    private ShiftDefinition(
        Guid id,
        Guid propertyId,
        string code,
        string name,
        TimeOnly startLocalTime,
        TimeOnly endLocalTime,
        bool endsNextDay,
        int breakMinutes,
        string actorUserId,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        PropertyId = propertyId;
        Code = code;
        Name = name;
        StartLocalTime = startLocalTime;
        EndLocalTime = endLocalTime;
        EndsNextDay = endsNextDay;
        BreakMinutes = breakMinutes;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = actorUserId;
        UpdatedAtUtc = createdAtUtc;
        UpdatedByUserId = actorUserId;
    }

    public Guid Id { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public TimeOnly StartLocalTime { get; private set; }
    public TimeOnly EndLocalTime { get; private set; }
    public bool EndsNextDay { get; private set; }
    public int BreakMinutes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public string CreatedByUserId { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public string UpdatedByUserId { get; private set; }

    public int GrossMinutes => ShiftDuration.GrossMinutes(StartLocalTime, EndLocalTime, EndsNextDay);

    public int PlannedNetMinutes => GrossMinutes - BreakMinutes;

    public static bool TryCreate(
        Guid id,
        Guid propertyId,
        string? code,
        string? name,
        TimeOnly startLocalTime,
        TimeOnly endLocalTime,
        bool endsNextDay,
        int breakMinutes,
        string actorUserId,
        DateTimeOffset createdAtUtc,
        out ShiftDefinition? definition,
        out string? field,
        out string? errorCode)
    {
        definition = null;
        if (!TryNormalizeCode(code, out var normalizedCode, out field, out errorCode))
        {
            return false;
        }

        if (!TryNormalizeName(name, out var normalizedName, out field, out errorCode))
        {
            return false;
        }

        if (!ShiftDuration.TryValidateTimes(startLocalTime, endLocalTime, endsNextDay, out field, out errorCode))
        {
            return false;
        }

        var gross = ShiftDuration.GrossMinutes(startLocalTime, endLocalTime, endsNextDay);
        if (!ShiftDuration.TryValidateBreak(breakMinutes, gross, out field, out errorCode))
        {
            return false;
        }

        definition = new ShiftDefinition(
            id,
            propertyId,
            normalizedCode,
            normalizedName,
            startLocalTime,
            endLocalTime,
            endsNextDay,
            breakMinutes,
            actorUserId,
            createdAtUtc);
        field = null;
        errorCode = null;
        return true;
    }

    public bool TryRename(string? name, string actorUserId, DateTimeOffset utcNow, out string? field, out string? errorCode)
    {
        if (!TryNormalizeName(name, out var normalized, out field, out errorCode))
        {
            return false;
        }

        Name = normalized;
        Touch(actorUserId, utcNow);
        return true;
    }

    /// <summary>
    /// After first ScheduleEntry (live or history) usage, Start/End/EndsNextDay/BreakMinutes are locked
    /// so historical schedule interpretation is not rewritten. Name and IsActive remain editable.
    /// </summary>
    public bool TryUpdateSemanticTimes(
        TimeOnly startLocalTime,
        TimeOnly endLocalTime,
        bool endsNextDay,
        int breakMinutes,
        bool hasScheduleUsage,
        string actorUserId,
        DateTimeOffset utcNow,
        out string? field,
        out string? errorCode)
    {
        field = null;
        errorCode = null;

        var unchanged = StartLocalTime == startLocalTime
            && EndLocalTime == endLocalTime
            && EndsNextDay == endsNextDay
            && BreakMinutes == breakMinutes;
        if (unchanged)
        {
            Touch(actorUserId, utcNow);
            return true;
        }

        if (hasScheduleUsage)
        {
            field = ScheduleValidation.Fields.StartLocalTime;
            errorCode = ScheduleValidation.Codes.ShiftDefinitionSemanticFieldsLocked;
            return false;
        }

        if (!ShiftDuration.TryValidateTimes(startLocalTime, endLocalTime, endsNextDay, out field, out errorCode))
        {
            return false;
        }

        var gross = ShiftDuration.GrossMinutes(startLocalTime, endLocalTime, endsNextDay);
        if (!ShiftDuration.TryValidateBreak(breakMinutes, gross, out field, out errorCode))
        {
            return false;
        }

        StartLocalTime = startLocalTime;
        EndLocalTime = endLocalTime;
        EndsNextDay = endsNextDay;
        BreakMinutes = breakMinutes;
        Touch(actorUserId, utcNow);
        return true;
    }

    public void SetActive(bool isActive, string actorUserId, DateTimeOffset utcNow)
    {
        if (IsActive == isActive)
        {
            return;
        }

        IsActive = isActive;
        Touch(actorUserId, utcNow);
    }

    private void Touch(string actorUserId, DateTimeOffset utcNow)
    {
        UpdatedByUserId = actorUserId;
        UpdatedAtUtc = utcNow;
    }

    public static bool TryNormalizeCode(string? code, out string normalized, out string? field, out string? errorCode)
    {
        normalized = string.Empty;
        field = ScheduleValidation.Fields.Code;
        if (string.IsNullOrWhiteSpace(code))
        {
            errorCode = ScheduleValidation.Codes.ShiftDefinitionCodeRequired;
            return false;
        }

        var trimmed = code.Trim().ToLowerInvariant();
        if (trimmed.Length > CodeMaxLength)
        {
            errorCode = ScheduleValidation.Codes.ShiftDefinitionCodeTooLong;
            return false;
        }

        normalized = trimmed;
        field = null;
        errorCode = null;
        return true;
    }

    public static bool TryNormalizeName(string? name, out string normalized, out string? field, out string? errorCode)
    {
        normalized = string.Empty;
        field = ScheduleValidation.Fields.Name;
        if (string.IsNullOrWhiteSpace(name))
        {
            errorCode = ScheduleValidation.Codes.ShiftDefinitionNameRequired;
            return false;
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameMaxLength)
        {
            errorCode = ScheduleValidation.Codes.ShiftDefinitionNameTooLong;
            return false;
        }

        normalized = trimmed;
        field = null;
        errorCode = null;
        return true;
    }

    public static string NormalizeCodeForLookup(string code) =>
        code.Trim().ToLower(CultureInfo.InvariantCulture);
}
