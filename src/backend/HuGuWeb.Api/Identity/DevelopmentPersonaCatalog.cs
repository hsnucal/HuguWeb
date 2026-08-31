using HuGuWeb.Api.Authorization;
using HuGuWeb.Workforce.Infrastructure.Seeding;

namespace HuGuWeb.Api.Identity;

public sealed record DevelopmentPersonaDefinition(
    string Email,
    string RoleCode,
    IReadOnlyList<string> Permissions,
    Guid? PropertyId,
    IReadOnlyList<string>? AdditionalRoleCodes = null,
    Guid? LinkedEmployeeId = null,
    Guid? LinkedAccountLinkId = null,
    IReadOnlyList<string>? DepartmentScopeCodes = null)
{
    public IReadOnlyList<string> AssignedRoleCodes
    {
        get
        {
            if (AdditionalRoleCodes is null || AdditionalRoleCodes.Count == 0)
            {
                return [RoleCode];
            }

            var codes = new List<string>(1 + AdditionalRoleCodes.Count) { RoleCode };
            foreach (var code in AdditionalRoleCodes)
            {
                if (!codes.Contains(code, StringComparer.Ordinal))
                {
                    codes.Add(code);
                }
            }

            return codes;
        }
    }
}

public static class DevelopmentPersonaCatalog
{
    public const string BroadEmailKey = "DevelopmentUser:Email";
    public const string BroadPasswordKey = "DevelopmentUser:Password";
    public const string DefaultPasswordKey = "DevelopmentUsers:DefaultPassword";
    public const string DefaultBroadEmail = "dev@localhost";

    public static readonly IReadOnlyList<string> AllDevelopmentPermissions = PermissionCatalog.All;

    public static readonly IReadOnlyList<string> BroadPermissions = PermissionCatalog.All;

    private static readonly string[] LeaveSelfServiceRole = [SystemRoleTemplates.EmployeeLeaveSelfService];

    public static readonly DevelopmentPersonaDefinition HumanResourcesManager = new(
        "hr.manager@localhost",
        SystemRoleTemplates.HrManager,
        SystemRoleTemplates.HumanResourcesPermissions,
        DevelopmentWorkforceSeeder.AnkaraPropertyId,
        AdditionalRoleCodes: LeaveSelfServiceRole,
        LinkedEmployeeId: DevelopmentPersonaEmployeeFixtures.HrManagerEmployeeId,
        LinkedAccountLinkId: DevelopmentPersonaEmployeeFixtures.HrManagerLinkId);

    public static readonly DevelopmentPersonaDefinition HotelBHumanResourcesManager = new(
        "hr.antalya@localhost",
        SystemRoleTemplates.HrManager,
        SystemRoleTemplates.HumanResourcesPermissions,
        DevelopmentWorkforceSeeder.AntalyaPropertyId,
        AdditionalRoleCodes: LeaveSelfServiceRole,
        LinkedEmployeeId: DevelopmentPersonaEmployeeFixtures.AntalyaHrManagerEmployeeId,
        LinkedAccountLinkId: DevelopmentPersonaEmployeeFixtures.AntalyaHrManagerLinkId);

    /// <summary>Non-employee corporate/system persona — no EmployeeAccountLink.</summary>
    public static readonly DevelopmentPersonaDefinition CorporateHumanResources = new(
        "hr.corporate@localhost",
        SystemRoleTemplates.CorporateHr,
        SystemRoleTemplates.HumanResourcesPermissions,
        PropertyId: null);

    public static readonly DevelopmentPersonaDefinition RoomOperationsAttendant = new(
        "roomops.attendant@localhost",
        SystemRoleTemplates.RoomAttendant,
        [RoomOperationsPermissions.Read, RoomOperationsPermissions.Manage],
        DevelopmentWorkforceSeeder.AnkaraPropertyId,
        AdditionalRoleCodes: LeaveSelfServiceRole,
        LinkedEmployeeId: DevelopmentPersonaEmployeeFixtures.RoomAttendantEmployeeId,
        LinkedAccountLinkId: DevelopmentPersonaEmployeeFixtures.RoomAttendantLinkId);

    public static readonly DevelopmentPersonaDefinition RoomOperationsInspector = new(
        "roomops.inspector@localhost",
        SystemRoleTemplates.RoomInspector,
        [RoomOperationsPermissions.Read, RoomOperationsPermissions.Inspect],
        DevelopmentWorkforceSeeder.AnkaraPropertyId,
        AdditionalRoleCodes: LeaveSelfServiceRole,
        LinkedEmployeeId: DevelopmentPersonaEmployeeFixtures.RoomInspectorEmployeeId,
        LinkedAccountLinkId: DevelopmentPersonaEmployeeFixtures.RoomInspectorLinkId);

    public static readonly DevelopmentPersonaDefinition RoomOperationsManager = new(
        "roomops.manager@localhost",
        SystemRoleTemplates.RoomOperationsManager,
        [RoomOperationsPermissions.Read, RoomOperationsPermissions.Manage, RoomOperationsPermissions.Inspect],
        DevelopmentWorkforceSeeder.AnkaraPropertyId,
        AdditionalRoleCodes: LeaveSelfServiceRole,
        LinkedEmployeeId: DevelopmentPersonaEmployeeFixtures.RoomOpsManagerEmployeeId,
        LinkedAccountLinkId: DevelopmentPersonaEmployeeFixtures.RoomOpsManagerLinkId);

    /// <summary>
    /// Operational hotel employee persona: maintenance resolve + leave self-service.
    /// Linked via deterministic EmployeeAccountLink only (never email/personnel-number lookup).
    /// </summary>
    public static readonly DevelopmentPersonaDefinition MaintenanceTechnician = new(
        "maintenance.technician@localhost",
        SystemRoleTemplates.MaintenanceTechnician,
        [
            MaintenancePermissions.Read,
            MaintenancePermissions.Resolve,
            ..SystemRoleTemplates.EmployeeLeaveSelfServicePermissions
        ],
        DevelopmentWorkforceSeeder.AnkaraPropertyId,
        AdditionalRoleCodes: LeaveSelfServiceRole,
        LinkedEmployeeId: DevelopmentPersonaEmployeeFixtures.MaintenanceTechnicianEmployeeId,
        LinkedAccountLinkId: DevelopmentPersonaEmployeeFixtures.MaintenanceTechnicianLinkId);

    public static readonly DevelopmentPersonaDefinition MaintenanceManager = new(
        "maintenance.manager@localhost",
        SystemRoleTemplates.MaintenanceManager,
        [
            MaintenancePermissions.Read,
            MaintenancePermissions.Manage,
            MaintenancePermissions.Resolve,
            ..SystemRoleTemplates.DepartmentLeaveApproverPermissions
        ],
        DevelopmentWorkforceSeeder.AnkaraPropertyId,
        AdditionalRoleCodes:
        [
            SystemRoleTemplates.EmployeeLeaveSelfService,
            SystemRoleTemplates.DepartmentLeaveApprover
        ],
        LinkedEmployeeId: DevelopmentPersonaEmployeeFixtures.MaintenanceManagerEmployeeId,
        LinkedAccountLinkId: DevelopmentPersonaEmployeeFixtures.MaintenanceManagerLinkId,
        DepartmentScopeCodes: ["ENG"]);

    public static IReadOnlyList<DevelopmentPersonaDefinition> AdditionalPersonas { get; } =
    [
        HumanResourcesManager,
        HotelBHumanResourcesManager,
        CorporateHumanResources,
        RoomOperationsAttendant,
        RoomOperationsInspector,
        RoomOperationsManager,
        MaintenanceTechnician,
        MaintenanceManager
    ];

    public static DevelopmentPersonaDefinition Broad(string? configuredEmail)
    {
        var email = string.IsNullOrWhiteSpace(configuredEmail)
            ? DefaultBroadEmail
            : configuredEmail.Trim();
        return new DevelopmentPersonaDefinition(
            email,
            SystemRoleTemplates.DevelopmentSuperuser,
            BroadPermissions,
            PropertyId: null);
    }
}
