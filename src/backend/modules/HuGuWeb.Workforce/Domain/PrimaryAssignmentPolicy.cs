namespace HuGuWeb.Workforce.Domain;

public static class PrimaryAssignments
{
    public static bool HasOverlap(IReadOnlyList<Assignment> assignments)
    {
        var primaries = assignments
            .Where(assignment => assignment.Kind == AssignmentKind.Primary)
            .OrderBy(assignment => assignment.StartDate)
            .ToArray();

        for (var i = 0; i < primaries.Length; i++)
        {
            for (var j = i + 1; j < primaries.Length; j++)
            {
                if (primaries[i].Period.Overlaps(primaries[j].Period))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public static Assignment? Covering(IReadOnlyList<Assignment> assignments, DateOnly date) =>
        assignments
            .Where(assignment => assignment.Kind == AssignmentKind.Primary && assignment.Covers(date))
            .OrderByDescending(assignment => assignment.StartDate)
            .FirstOrDefault();

    public static IReadOnlyList<Assignment> OrderedPrimaries(IReadOnlyList<Assignment> assignments) =>
        assignments
            .Where(assignment => assignment.Kind == AssignmentKind.Primary)
            .OrderBy(assignment => assignment.StartDate)
            .ThenBy(assignment => assignment.Id)
            .ToArray();
}
