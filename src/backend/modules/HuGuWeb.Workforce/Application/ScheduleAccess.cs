namespace HuGuWeb.Workforce.Application;

/// <summary>
/// Schedule access for a resolved workplace (Assignment → Department → Property) on a target date.
/// Property-wide actors pass <paramref name="allowedDepartmentIds"/> as null.
/// Department-limited actors pass the set of authorized Department ids (multi-department supported).
/// Historical authorization must use the Assignment that covers the schedule date — never current assignment.
/// </summary>
public static class ScheduleAccess
{
    /// <summary>
    /// Null <paramref name="allowedDepartmentIds"/> means Property-wide (all departments in the property).
    /// Non-null means only listed departments (may be empty → deny all).
    /// </summary>
    public static bool AllowsWorkplace(
        Guid? scopedPropertyId,
        IReadOnlySet<Guid>? allowedDepartmentIds,
        Guid workplacePropertyId,
        Guid workplaceDepartmentId)
    {
        if (scopedPropertyId is { } propertyId && workplacePropertyId != propertyId)
        {
            return false;
        }

        if (allowedDepartmentIds is null)
        {
            return true;
        }

        return allowedDepartmentIds.Contains(workplaceDepartmentId);
    }
}
