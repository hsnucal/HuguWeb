namespace HuGuWeb.Workforce.Domain;

public static class PropertyLocalCalendar
{
    public static DateOnly Today(DateTimeOffset utcNow, string timeZoneId)
    {
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var zone))
        {
            zone = TimeZoneInfo.Utc;
        }

        var local = TimeZoneInfo.ConvertTime(utcNow, zone);
        return DateOnly.FromDateTime(local.DateTime);
    }
}
