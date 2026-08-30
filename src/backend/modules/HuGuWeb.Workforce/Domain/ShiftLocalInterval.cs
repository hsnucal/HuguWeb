namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// Local wall-clock planned interval derived from ScheduleDate + ShiftDefinition times.
/// Never persisted as UTC; Property.TimeZoneId remains the timezone source for future HR-07.
/// </summary>
public readonly record struct ShiftLocalInterval(
    DateOnly StartLocalDate,
    TimeOnly StartLocalTime,
    DateOnly EndLocalDate,
    TimeOnly EndLocalTime,
    bool EndsNextDay,
    int BreakMinutes,
    int GrossMinutes,
    int PlannedNetMinutes)
{
    public static ShiftLocalInterval From(DateOnly scheduleDate, ShiftDefinition definition)
    {
        var endDate = definition.EndsNextDay ? scheduleDate.AddDays(1) : scheduleDate;
        return new ShiftLocalInterval(
            scheduleDate,
            definition.StartLocalTime,
            endDate,
            definition.EndLocalTime,
            definition.EndsNextDay,
            definition.BreakMinutes,
            definition.GrossMinutes,
            definition.PlannedNetMinutes);
    }
}

/// <summary>
/// Duration helpers for Property-local planned shift times. Times are wall-clock intent only.
/// DST conversion is deferred to HR-07 attendance; HR-06A stores local schedule intent.
/// </summary>
public static class ShiftDuration
{
    public static int GrossMinutes(TimeOnly start, TimeOnly end, bool endsNextDay)
    {
        var startMinutes = (int)start.ToTimeSpan().TotalMinutes;
        var endMinutes = (int)end.ToTimeSpan().TotalMinutes;
        if (endsNextDay)
        {
            return (24 * 60 - startMinutes) + endMinutes;
        }

        return endMinutes - startMinutes;
    }

    public static bool TryValidateTimes(
        TimeOnly start,
        TimeOnly end,
        bool endsNextDay,
        out string? field,
        out string? errorCode)
    {
        field = null;
        errorCode = null;

        // MVP: Start == End is invalid (includes deferred 24h shifts).
        if (start == end)
        {
            field = ScheduleValidation.Fields.StartLocalTime;
            errorCode = ScheduleValidation.Codes.ShiftDefinitionInvalidTime;
            return false;
        }

        // When End <= Start in clock order, EndsNextDay is mandatory (same-day End <= Start is invalid).
        if (end <= start && !endsNextDay)
        {
            field = ScheduleValidation.Fields.EndsNextDay;
            errorCode = ScheduleValidation.Codes.ShiftDefinitionInvalidTime;
            return false;
        }

        var gross = GrossMinutes(start, end, endsNextDay);
        if (gross <= 0)
        {
            field = ScheduleValidation.Fields.EndLocalTime;
            errorCode = ScheduleValidation.Codes.ShiftDefinitionInvalidTime;
            return false;
        }

        return true;
    }

    public static bool TryValidateBreak(int breakMinutes, int grossMinutes, out string? field, out string? errorCode)
    {
        field = ScheduleValidation.Fields.BreakMinutes;
        if (breakMinutes < 0 || breakMinutes >= grossMinutes)
        {
            errorCode = ScheduleValidation.Codes.ShiftDefinitionInvalidBreak;
            return false;
        }

        field = null;
        errorCode = null;
        return true;
    }
}
