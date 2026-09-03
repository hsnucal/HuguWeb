namespace HuGuWeb.Api.Authorization;

public sealed record SystemRoleTemplate(
    Guid Id,
    string Code,
    string Name,
    AuthorizationScopeType ScopeType,
    IReadOnlyList<string> Permissions);

public static class SystemRoleTemplates
{
    public static readonly Guid DevelopmentSuperuserId = Guid.Parse("b1e1c0de-0001-4000-8000-000000000001");
    public static readonly Guid HrManagerId = Guid.Parse("b1e1c0de-0001-4000-8000-000000000002");
    public static readonly Guid HrSpecialistId = Guid.Parse("b1e1c0de-0001-4000-8000-000000000003");
    public static readonly Guid RoomOperationsManagerId = Guid.Parse("b1e1c0de-0001-4000-8000-000000000004");
    public static readonly Guid RoomAttendantId = Guid.Parse("b1e1c0de-0001-4000-8000-000000000005");
    public static readonly Guid RoomInspectorId = Guid.Parse("b1e1c0de-0001-4000-8000-000000000006");
    public static readonly Guid MaintenanceManagerId = Guid.Parse("b1e1c0de-0001-4000-8000-000000000007");
    public static readonly Guid MaintenanceTechnicianId = Guid.Parse("b1e1c0de-0001-4000-8000-000000000008");
    public static readonly Guid CorporateHrId = Guid.Parse("b1e1c0de-0001-4000-8000-000000000009");
    public static readonly Guid EmployeeLeaveSelfServiceId = Guid.Parse("b1e1c0de-0001-4000-8000-00000000000a");
    public static readonly Guid DepartmentLeaveApproverId = Guid.Parse("b1e1c0de-0001-4000-8000-00000000000b");
    public static readonly Guid DepartmentSchedulerId = Guid.Parse("b1e1c0de-0001-4000-8000-00000000000c");

    public const string DevelopmentSuperuser = "development-superuser";
    public const string HrManager = "hr-manager";
    public const string HrSpecialist = "hr-specialist";
    public const string RoomOperationsManager = "room-operations-manager";
    public const string RoomAttendant = "room-attendant";
    public const string RoomInspector = "room-inspector";
    public const string MaintenanceManager = "maintenance-manager";
    public const string MaintenanceTechnician = "maintenance-technician";
    public const string CorporateHr = "hr-corporate";
    public const string EmployeeLeaveSelfService = "employee-leave-self-service";
    public const string DepartmentLeaveApprover = "department-leave-approver";
    public const string DepartmentScheduler = "department-scheduler";

    public static readonly IReadOnlyList<string> HumanResourcesPermissions =
    [
        WorkforcePermissions.Read,
        WorkforcePermissions.Manage,
        HrEmployeePermissions.Read,
        HrEmployeePermissions.Manage,
        HrEmployeePermissions.SensitiveRead,
        HrLeavePermissions.Read,
        HrLeavePermissions.Manage,
        HrLeavePermissions.Approve,
        HrSchedulePermissions.Read,
        HrSchedulePermissions.Manage,
        HrAttendancePermissions.Read,
        HrAttendancePermissions.Manage,
        HrShiftDefinitionPermissions.Read,
        HrShiftDefinitionPermissions.Manage
    ];

    /// <summary>
    /// Typical department operational scheduler/approver: schedules + department leave approval.
    /// Does not grant HR leave manage. Schedule portion is bound to <see cref="DepartmentScheduler"/>;
    /// leave approval portion is bound to <see cref="DepartmentLeaveApprover"/>.
    /// </summary>
    public static readonly IReadOnlyList<string> DepartmentSchedulerPermissions =
    [
        HrSchedulePermissions.Read,
        HrSchedulePermissions.Manage,
        HrShiftDefinitionPermissions.Read,
        HrLeavePermissions.Read,
        HrLeavePermissions.Approve,
        HrAttendancePermissions.Read
    ];

    /// <summary>
    /// Department-scoped shift schedule + shift definition read. Does not grant leave approve/manage.
    /// Assign alongside operational manager roles; AUTH-02 department scopes provide WHERE.
    /// </summary>
    public static readonly IReadOnlyList<string> DepartmentSchedulerOnlyPermissions =
    [
        HrSchedulePermissions.Read,
        HrSchedulePermissions.Manage,
        HrShiftDefinitionPermissions.Read,
        HrAttendancePermissions.Read
    ];

    /// <summary>
    /// Department-stage leave approve/reject (+ list read). Does not grant HR final manage.
    /// Assign alongside operational manager roles; AUTH-02 department scopes provide WHERE.
    /// </summary>
    public static readonly IReadOnlyList<string> DepartmentLeaveApproverPermissions =
    [
        HrLeavePermissions.Read,
        HrLeavePermissions.Approve
    ];

    /// <summary>
    /// Employee self-service leave. Bound to system role <see cref="EmployeeLeaveSelfService"/>;
    /// assign alongside operational employee roles (not HR admin templates).
    /// </summary>
    public static readonly IReadOnlyList<string> EmployeeLeaveSelfServicePermissions =
    [
        HrLeavePermissions.Request
    ];

    public static IReadOnlyList<SystemRoleTemplate> All { get; } =
    [
        new(
            DevelopmentSuperuserId,
            DevelopmentSuperuser,
            "Development Superuser",
            AuthorizationScopeType.Organization,
            PermissionCatalog.All),
        new(HrManagerId, HrManager, "HR Manager", AuthorizationScopeType.Property, HumanResourcesPermissions),
        new(HrSpecialistId, HrSpecialist, "HR Specialist", AuthorizationScopeType.Property, HumanResourcesPermissions),
        new(
            CorporateHrId,
            CorporateHr,
            "Corporate HR",
            AuthorizationScopeType.Organization,
            HumanResourcesPermissions),
        new(
            RoomOperationsManagerId,
            RoomOperationsManager,
            "Room Operations Manager",
            AuthorizationScopeType.Property,
            [RoomOperationsPermissions.Read, RoomOperationsPermissions.Manage, RoomOperationsPermissions.Inspect]),
        new(
            RoomAttendantId,
            RoomAttendant,
            "Room Attendant",
            AuthorizationScopeType.Property,
            [RoomOperationsPermissions.Read, RoomOperationsPermissions.Manage]),
        new(
            RoomInspectorId,
            RoomInspector,
            "Room Inspector",
            AuthorizationScopeType.Property,
            [RoomOperationsPermissions.Read, RoomOperationsPermissions.Inspect]),
        new(
            MaintenanceManagerId,
            MaintenanceManager,
            "Maintenance Manager",
            AuthorizationScopeType.Property,
            [MaintenancePermissions.Read, MaintenancePermissions.Manage, MaintenancePermissions.Resolve]),
        new(
            MaintenanceTechnicianId,
            MaintenanceTechnician,
            "Maintenance Technician",
            AuthorizationScopeType.Property,
            [MaintenancePermissions.Read, MaintenancePermissions.Resolve]),
        new(
            EmployeeLeaveSelfServiceId,
            EmployeeLeaveSelfService,
            "Employee Leave Self-Service",
            AuthorizationScopeType.Property,
            EmployeeLeaveSelfServicePermissions),
        new(
            DepartmentLeaveApproverId,
            DepartmentLeaveApprover,
            "Department Leave Approver",
            AuthorizationScopeType.Property,
            DepartmentLeaveApproverPermissions),
        new(
            DepartmentSchedulerId,
            DepartmentScheduler,
            "Department Scheduler",
            AuthorizationScopeType.Property,
            DepartmentSchedulerOnlyPermissions)
    ];

    public static SystemRoleTemplate? ByCode(string code) =>
        All.FirstOrDefault(item => item.Code.Equals(code, StringComparison.Ordinal));
}
