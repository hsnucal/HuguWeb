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

    public const string DevelopmentSuperuser = "development-superuser";
    public const string HrManager = "hr-manager";
    public const string HrSpecialist = "hr-specialist";
    public const string RoomOperationsManager = "room-operations-manager";
    public const string RoomAttendant = "room-attendant";
    public const string RoomInspector = "room-inspector";
    public const string MaintenanceManager = "maintenance-manager";
    public const string MaintenanceTechnician = "maintenance-technician";
    public const string CorporateHr = "hr-corporate";

    public static readonly IReadOnlyList<string> HumanResourcesPermissions =
    [
        WorkforcePermissions.Read,
        WorkforcePermissions.Manage,
        HrEmployeePermissions.Read,
        HrEmployeePermissions.Manage,
        HrEmployeePermissions.SensitiveRead,
        HrLeavePermissions.Read,
        HrLeavePermissions.Manage,
        HrSchedulePermissions.Read,
        HrSchedulePermissions.Manage,
        HrShiftDefinitionPermissions.Read,
        HrShiftDefinitionPermissions.Manage
    ];

    /// <summary>
    /// Typical department operational scheduler: assign schedules + read definitions.
    /// Does not grant ShiftDefinition catalogue management. Not bound to a system role until
    /// Department membership scope exists in the authorization schema.
    /// </summary>
    public static readonly IReadOnlyList<string> DepartmentSchedulerPermissions =
    [
        HrSchedulePermissions.Read,
        HrSchedulePermissions.Manage,
        HrShiftDefinitionPermissions.Read
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
            [MaintenancePermissions.Read, MaintenancePermissions.Resolve])
    ];

    public static SystemRoleTemplate? ByCode(string code) =>
        All.FirstOrDefault(item => item.Code.Equals(code, StringComparison.Ordinal));
}
