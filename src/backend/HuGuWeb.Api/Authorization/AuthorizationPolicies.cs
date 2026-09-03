namespace HuGuWeb.Api.Authorization;

public static class AuthorizationPolicies
{
    public const string Authenticated = "Authenticated";
    public const string WorkforceRead = "WorkforceRead";
    public const string WorkforceManage = "WorkforceManage";
    public const string RoomOperationsRead = "RoomOperationsRead";
    public const string RoomOperationsManage = "RoomOperationsManage";
    public const string RoomOperationsInspect = "RoomOperationsInspect";
    public const string MaintenanceRead = "MaintenanceRead";
    public const string MaintenanceManage = "MaintenanceManage";
    public const string MaintenanceResolve = "MaintenanceResolve";
    public const string HrEmployeeRead = "HrEmployeeRead";
    public const string HrEmployeeManage = "HrEmployeeManage";
    public const string HrEmployeeHire = "HrEmployeeHire";
    public const string HrLeaveRead = "HrLeaveRead";
    public const string HrLeaveManage = "HrLeaveManage";
    public const string HrLeaveRequest = "HrLeaveRequest";
    public const string HrLeaveApprove = "HrLeaveApprove";
    public const string HrScheduleRead = "HrScheduleRead";
    public const string HrScheduleManage = "HrScheduleManage";
    public const string HrShiftDefinitionRead = "HrShiftDefinitionRead";
    public const string HrShiftDefinitionManage = "HrShiftDefinitionManage";
    public const string HrAttendanceRead = "HrAttendanceRead";
    public const string HrAttendanceManage = "HrAttendanceManage";
    public const string AuthorizationUsersManage = "AuthorizationUsersManage";
    public const string AuthorizationRolesManage = "AuthorizationRolesManage";
}
