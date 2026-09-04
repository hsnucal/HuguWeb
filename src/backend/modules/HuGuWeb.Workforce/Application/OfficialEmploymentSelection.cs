using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

internal static class OfficialEmploymentSelection
{
    public static WorkforceResult<Employment> ForEmployee(IReadOnlyList<Employment> employments)
    {
        var open = employments.Where(item => !item.IsEnded).ToArray();
        if (open.Length > 1)
        {
            return WorkforceError.MultipleOpenEmployments();
        }

        if (open.Length == 1)
        {
            return open[0];
        }

        var latest = employments
            .OrderByDescending(item => item.StartDate)
            .ThenByDescending(item => item.Id)
            .FirstOrDefault();
        if (latest is null)
        {
            return WorkforceError.EmploymentNotFound();
        }

        return latest;
    }
}

internal static class EmploymentPropertyContext
{
    public static WorkforceResult<Guid> Resolve(
        Employment employment,
        IReadOnlyList<Assignment> assignments,
        IReadOnlyDictionary<Guid, Department> departments,
        DateOnly today)
    {
        var primaries = PrimaryAssignments.OrderedPrimaries(assignments);
        if (primaries.Count == 0)
        {
            return WorkforceError.EmploymentPropertyUnresolved();
        }

        Assignment? relevant;
        if (!employment.IsEnded)
        {
            relevant = PrimaryAssignments.Covering(assignments, today)
                ?? (employment.StartDate > today
                    ? PrimaryAssignments.Covering(assignments, employment.StartDate)
                    : null);
        }
        else
        {
            relevant = primaries[^1];
        }

        if (relevant is null || !departments.TryGetValue(relevant.DepartmentId, out var department))
        {
            return WorkforceError.EmploymentPropertyUnresolved();
        }

        return department.PropertyId;
    }
}
