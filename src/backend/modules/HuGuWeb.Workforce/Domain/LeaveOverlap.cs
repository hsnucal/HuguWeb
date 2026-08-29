namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// HR-05A overlap rule: two <see cref="LeaveRecordStatus.Recorded"/> records for the same
/// Employment whose inclusive date ranges intersect are invalid. Cancelled records are ignored.
/// There is no time-of-day segment, so two half-days on the same date overlap.
/// </summary>
public static class LeaveOverlap
{
    public static bool RangesOverlap(DateOnly startA, DateOnly endA, DateOnly startB, DateOnly endB) =>
        startA <= endB && startB <= endA;

    public static bool OverlapsAnyRecorded(
        IEnumerable<LeaveRecord> existing,
        DateOnly startDate,
        DateOnly endDate,
        Guid? ignoreRecordId = null)
    {
        foreach (var record in existing)
        {
            if (record.Status != LeaveRecordStatus.Recorded)
            {
                continue;
            }

            if (ignoreRecordId is { } ignore && record.Id == ignore)
            {
                continue;
            }

            if (RangesOverlap(startDate, endDate, record.StartDate, record.EndDate))
            {
                return true;
            }
        }

        return false;
    }
}
