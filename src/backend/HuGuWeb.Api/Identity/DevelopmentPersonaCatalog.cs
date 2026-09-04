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
    Guid? MembershipId = null,
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
        LinkedAccountLinkId: DevelopmentPersonaEmployeeFixtures.HrManagerLinkId,
        MembershipId: Guid.Parse("b1e1c0de-0006-4000-8000-000000000002"));

    public static readonly DevelopmentPersonaDefinition HotelBHumanResourcesManager = new(
        "hr.antalya@localhost",
        SystemRoleTemplates.HrManager,
        SystemRoleTemplates.HumanResourcesPermissions,
        DevelopmentWorkforceSeeder.AntalyaPropertyId,
        AdditionalRoleCodes: LeaveSelfServiceRole,
        LinkedEmployeeId: DevelopmentPersonaEmployeeFixtures.AntalyaHrManagerEmployeeId,
        LinkedAccountLinkId: DevelopmentPersonaEmployeeFixtures.AntalyaHrManagerLinkId,
        MembershipId: Guid.Parse("b1e1c0de-0006-4000-8000-000000000007"));

    /// <summary>Non-employee corporate/system persona — no EmployeeAccountLink.</summary>
    public static readonly DevelopmentPersonaDefinition CorporateHumanResources = new(
        "hr.corporate@localhost",
        SystemRoleTemplates.CorporateHr,
        SystemRoleTemplates.HumanResourcesPermissions,
        PropertyId: null,
        MembershipId: Guid.Parse("b1e1c0de-0006-4000-8000-000000000009"));

    public static readonly DevelopmentPersonaDefinition RoomOperationsAttendant = new(
        "roomops.attendant@localhost",
        SystemRoleTemplates.RoomAttendant,
        [RoomOperationsPermissions.Read, RoomOperationsPermissions.Manage],
        DevelopmentWorkforceSeeder.AnkaraPropertyId,
        AdditionalRoleCodes: LeaveSelfServiceRole,
        LinkedEmployeeId: DevelopmentPersonaEmployeeFixtures.RoomAttendantEmployeeId,
        LinkedAccountLinkId: DevelopmentPersonaEmployeeFixtures.RoomAttendantLinkId,
        MembershipId: Guid.Parse("b1e1c0de-0006-4000-8000-000000000003"));

    public static readonly DevelopmentPersonaDefinition RoomOperationsInspector = new(
        "roomops.inspector@localhost",
        SystemRoleTemplates.RoomInspector,
        [RoomOperationsPermissions.Read, RoomOperationsPermissions.Inspect],
        DevelopmentWorkforceSeeder.AnkaraPropertyId,
        AdditionalRoleCodes: LeaveSelfServiceRole,
        LinkedEmployeeId: DevelopmentPersonaEmployeeFixtures.RoomInspectorEmployeeId,
        LinkedAccountLinkId: DevelopmentPersonaEmployeeFixtures.RoomInspectorLinkId,
        MembershipId: Guid.Parse("b1e1c0de-0006-4000-8000-000000000005"));

    public static readonly DevelopmentPersonaDefinition RoomOperationsManager = new(
        "roomops.manager@localhost",
        SystemRoleTemplates.RoomOperationsManager,
        [
            RoomOperationsPermissions.Read,
            RoomOperationsPermissions.Manage,
            RoomOperationsPermissions.Inspect,
            ..SystemRoleTemplates.DepartmentLeaveApproverPermissions,
            ..SystemRoleTemplates.DepartmentSchedulerOnlyPermissions
        ],
        DevelopmentWorkforceSeeder.AnkaraPropertyId,
        AdditionalRoleCodes:
        [
            SystemRoleTemplates.EmployeeLeaveSelfService,
            SystemRoleTemplates.DepartmentLeaveApprover,
            SystemRoleTemplates.DepartmentScheduler
        ],
        LinkedEmployeeId: DevelopmentPersonaEmployeeFixtures.RoomOpsManagerEmployeeId,
        LinkedAccountLinkId: DevelopmentPersonaEmployeeFixtures.RoomOpsManagerLinkId,
        MembershipId: Guid.Parse("b1e1c0de-0006-4000-8000-000000000006"),
        DepartmentScopeCodes: ["HK"]);

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
        LinkedAccountLinkId: DevelopmentPersonaEmployeeFixtures.MaintenanceTechnicianLinkId,
        MembershipId: Guid.Parse("b1e1c0de-0006-4000-8000-000000000001"));

    public static readonly DevelopmentPersonaDefinition MaintenanceManager = new(
        "maintenance.manager@localhost",
        SystemRoleTemplates.MaintenanceManager,
        [
            MaintenancePermissions.Read,
            MaintenancePermissions.Manage,
            MaintenancePermissions.Resolve,
            ..SystemRoleTemplates.DepartmentLeaveApproverPermissions,
            ..SystemRoleTemplates.DepartmentSchedulerOnlyPermissions
        ],
        DevelopmentWorkforceSeeder.AnkaraPropertyId,
        AdditionalRoleCodes:
        [
            SystemRoleTemplates.EmployeeLeaveSelfService,
            SystemRoleTemplates.DepartmentLeaveApprover,
            SystemRoleTemplates.DepartmentScheduler
        ],
        LinkedEmployeeId: DevelopmentPersonaEmployeeFixtures.MaintenanceManagerEmployeeId,
        LinkedAccountLinkId: DevelopmentPersonaEmployeeFixtures.MaintenanceManagerLinkId,
        MembershipId: Guid.Parse("b1e1c0de-0006-4000-8000-000000000004"),
        DepartmentScopeCodes: ["ENG"]);

    public static readonly DevelopmentPersonaDefinition FrontOfficeReceptionist = new(
        "frontoffice.receptionist@localhost",
        SystemRoleTemplates.EmployeeLeaveSelfService,
        SystemRoleTemplates.EmployeeLeaveSelfServicePermissions,
        DevelopmentWorkforceSeeder.AnkaraPropertyId,
        LinkedEmployeeId: DevelopmentPersonaEmployeeFixtures.FrontOfficeReceptionistEmployeeId,
        LinkedAccountLinkId: DevelopmentPersonaEmployeeFixtures.FrontOfficeReceptionistLinkId,
        MembershipId: Guid.Parse("b1e1c0de-0006-4000-8000-000000000008"));

    public static IReadOnlyList<DevelopmentPersonaDefinition> AdditionalPersonas { get; } =
    [
        HumanResourcesManager,
        HotelBHumanResourcesManager,
        CorporateHumanResources,
        RoomOperationsAttendant,
        RoomOperationsInspector,
        RoomOperationsManager,
        MaintenanceTechnician,
        MaintenanceManager,
        FrontOfficeReceptionist
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
