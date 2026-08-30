using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

/// <summary>
/// Resolves the Employment that covers a calendar date for an employee. No current-open fallback for historical dates.
/// </summary>
internal static class ScheduleEmploymentResolver
{
    public static WorkforceResult<Employment> ResolveCovering(
        IReadOnlyList<Employment> employments,
        DateOnly scheduleDate)
    {
        var covering = employments
            .Where(item => item.Period.Contains(scheduleDate))
            .OrderByDescending(item => item.StartDate)
            .ToArray();

        if (covering.Length == 0)
        {
            return WorkforceError.ScheduleEmploymentNotCoveringDate();
        }

        if (covering.Length > 1)
        {
            return WorkforceError.MultipleOpenEmployments();
        }

        return covering[0];
    }
}

internal static class ScheduleWorkplaceResolver
{
    public static async Task<WorkforceResult<ScheduleWorkplaceContext>> ResolveAsync(
        IWorkforceStore store,
        Employment employment,
        DateOnly scheduleDate,
        CancellationToken cancellationToken)
    {
        if (!ScheduleEntry.IsWithinEmploymentPeriod(employment, scheduleDate))
        {
            return WorkforceError.ScheduleEmploymentNotCoveringDate();
        }

        var assignments = await store.ListAssignmentsAsync(employment.Id, cancellationToken);
        var assignment = EffectiveAssignmentResolver.ResolvePrimaryAssignmentOnDate(assignments, scheduleDate);
        if (assignment is null)
        {
            return WorkforceError.ScheduleAssignmentNotFound();
        }

        if (assignment.EmploymentId != employment.Id)
        {
            return WorkforceError.ScheduleAssignmentNotFound();
        }

        var department = await store.GetDepartmentAsync(assignment.DepartmentId, cancellationToken);
        if (department is null)
        {
            return WorkforceError.ScheduleAssignmentNotFound();
        }

        var property = await store.GetPropertyAsync(department.PropertyId, cancellationToken);
        if (property is null)
        {
            return WorkforceError.ScheduleAssignmentNotFound();
        }

        return new ScheduleWorkplaceContext(employment, assignment, department, property);
    }
}

public sealed record ScheduleWorkplaceContext(
    Employment Employment,
    Assignment Assignment,
    Department Department,
    Property Property);
