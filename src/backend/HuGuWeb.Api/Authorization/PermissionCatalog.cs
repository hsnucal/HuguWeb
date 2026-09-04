namespace HuGuWeb.Api.Authorization;

public static class PermissionCatalog
{
    public static readonly IReadOnlyList<string> All =
    [
        WorkforcePermissions.Read,
        WorkforcePermissions.Manage,
        HrEmployeePermissions.Read,
        HrEmployeePermissions.Manage,
        HrEmployeePermissions.SensitiveRead,
        HrLeavePermissions.Read,
        HrLeavePermissions.Manage,
        HrLeavePermissions.Request,
        HrLeavePermissions.Approve,
        HrSchedulePermissions.Read,
        HrSchedulePermissions.Manage,
        HrAttendancePermissions.Read,
        HrAttendancePermissions.Manage,
        HrAttendancePermissions.Close,
        HrShiftDefinitionPermissions.Read,
        HrShiftDefinitionPermissions.Manage,
        HrMovementPermissions.Read,
        HrMovementPermissions.Manage,
        HrMovementPermissions.Approve,
        RoomOperationsPermissions.Read,
        RoomOperationsPermissions.Manage,
        RoomOperationsPermissions.Inspect,
        MaintenancePermissions.Read,
        MaintenancePermissions.Manage,
        MaintenancePermissions.Resolve,
        AuthorizationPermissions.UsersManage,
        AuthorizationPermissions.RolesManage
    ];

    public static bool IsKnown(string? code) =>
        !string.IsNullOrWhiteSpace(code) && All.Contains(code, StringComparer.Ordinal);

    public static string DomainGroup(string code)
    {
        if (code.StartsWith("workforce.", StringComparison.Ordinal)
            || code.StartsWith("hr.", StringComparison.Ordinal))
        {
            return "hr";
        }

        if (code.StartsWith("room-operations.", StringComparison.Ordinal))
        {
            return "room-operations";
        }

        if (code.StartsWith("maintenance.", StringComparison.Ordinal))
        {
            return "technical-service";
        }

        return "authorization";
    }
}
