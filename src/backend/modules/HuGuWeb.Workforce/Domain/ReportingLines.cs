namespace HuGuWeb.Workforce.Domain;

public static class ReportingLines
{
    public static WorkforceReportingLine? Covering(
        IReadOnlyList<WorkforceReportingLine> lines,
        DateOnly date) =>
        lines
            .Where(line => line.Covers(date))
            .OrderByDescending(line => line.EffectiveFrom)
            .FirstOrDefault();

    public static bool HasOverlap(IReadOnlyList<WorkforceReportingLine> lines)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            for (var j = i + 1; j < lines.Count; j++)
            {
                if (lines[i].Period.Overlaps(lines[j].Period))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static bool WouldCreateCycle(
        Guid subordinateEmploymentId,
        Guid managerEmploymentId,
        DateOnly asOf,
        Func<Guid, WorkforceReportingLine?> coveringFor)
    {
        var seen = new HashSet<Guid> { subordinateEmploymentId };
        var current = managerEmploymentId;
        while (current != Guid.Empty)
        {
            if (!seen.Add(current))
            {
                return true;
            }

            var line = coveringFor(current);
            if (line is null)
            {
                break;
            }

            current = line.ManagerEmploymentId;
        }

        return false;
    }
}
