using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

/// <summary>
/// Authorization WHERE for leave requests uses the persisted request Assignment workplace,
/// not the employee's current assignment (historical ownership after transfer).
/// </summary>
public static class LeaveRequestWorkplaceAccess
{
    public static bool Allows(
        Guid? scopedPropertyId,
        IReadOnlySet<Guid>? allowedDepartmentIds,
        Guid assignmentPropertyId,
        Guid assignmentDepartmentId) =>
        ScheduleAccess.AllowsWorkplace(
            scopedPropertyId,
            allowedDepartmentIds,
            assignmentPropertyId,
            assignmentDepartmentId);
}
