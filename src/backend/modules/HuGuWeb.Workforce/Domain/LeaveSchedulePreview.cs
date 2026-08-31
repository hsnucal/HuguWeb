namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// HR-05B schedule preview over a leave request date range.
/// Uses HR-06 schedule facts only: Shift → Scheduled, RestDay → RestDay, missing row → Unscheduled.
/// Does not hardcode weekdays or public holidays.
/// </summary>
public static class LeaveSchedulePreview
{
    public const string StateScheduled = "Scheduled";
    public const string StateRestDay = "RestDay";
    public const string StateUnscheduled = "Unscheduled";

    public static LeaveSchedulePreviewResult Build(
        DateOnly startDate,
        DateOnly endDate,
        IEnumerable<ScheduleEntry> entries)
    {
        if (startDate > endDate)
        {
            return new LeaveSchedulePreviewResult([], SuggestedAmount: 0m, ScheduleIncomplete: false);
        }

        var byDate = entries
            .Where(item => item.ScheduleDate >= startDate && item.ScheduleDate <= endDate)
            .GroupBy(item => item.ScheduleDate)
            .ToDictionary(group => group.Key, group => group.First());

        var days = new List<LeaveSchedulePreviewDay>();
        var suggested = 0m;
        var incomplete = false;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (!byDate.TryGetValue(date, out var entry))
            {
                days.Add(new LeaveSchedulePreviewDay(date, StateUnscheduled, ChargeableCandidate: 0m));
                incomplete = true;
                continue;
            }

            if (entry.Kind == ScheduleEntryKind.RestDay)
            {
                days.Add(new LeaveSchedulePreviewDay(date, StateRestDay, ChargeableCandidate: 0m));
                continue;
            }

            // Shift (Scheduled)
            days.Add(new LeaveSchedulePreviewDay(date, StateScheduled, ChargeableCandidate: 1.0m));
            suggested += 1.0m;
        }

        return new LeaveSchedulePreviewResult(days, suggested, incomplete);
    }
}

public sealed record LeaveSchedulePreviewDay(DateOnly Date, string State, decimal ChargeableCandidate);

public sealed record LeaveSchedulePreviewResult(
    IReadOnlyList<LeaveSchedulePreviewDay> Days,
    decimal SuggestedAmount,
    bool ScheduleIncomplete);
