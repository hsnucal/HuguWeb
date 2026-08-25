using HuGuWeb.Api.Authorization;
using HuGuWeb.Workforce.Infrastructure.Seeding;

namespace HuGuWeb.Api.Identity;

public sealed record DevelopmentPersonaDefinition(
    string Email,
    string RoleCode,
    IReadOnlyList<string> Permissions,
    Guid? PropertyId);

public static class DevelopmentPersonaCatalog
{
    public const string BroadEmailKey = "DevelopmentUser:Email";
    public const string BroadPasswordKey = "DevelopmentUser:Password";
    public const string DefaultPasswordKey = "DevelopmentUsers:DefaultPassword";
    public const string DefaultBroadEmail = "dev@localhost";

    public static readonly IReadOnlyList<string> AllDevelopmentPermissions = PermissionCatalog.All;

    public static readonly IReadOnlyList<string> BroadPermissions = PermissionCatalog.All;

    public static readonly DevelopmentPersonaDefinition HumanResourcesManager = new(
        "hr.manager@localhost",
        SystemRoleTemplates.HrManager,
        SystemRoleTemplates.HumanResourcesPermissions,
        DevelopmentWorkforceSeeder.AnkaraPropertyId);

    public static readonly DevelopmentPersonaDefinition HotelBHumanResourcesManager = new(
        "hr.antalya@localhost",
        SystemRoleTemplates.HrManager,
        SystemRoleTemplates.HumanResourcesPermissions,
        DevelopmentWorkforceSeeder.AntalyaPropertyId);

    public static readonly DevelopmentPersonaDefinition CorporateHumanResources = new(
        "hr.corporate@localhost",
        SystemRoleTemplates.CorporateHr,
        SystemRoleTemplates.HumanResourcesPermissions,
        PropertyId: null);

    public static readonly DevelopmentPersonaDefinition RoomOperationsAttendant = new(
        "roomops.attendant@localhost",
        SystemRoleTemplates.RoomAttendant,
        [RoomOperationsPermissions.Read, RoomOperationsPermissions.Manage],
        DevelopmentWorkforceSeeder.AnkaraPropertyId);

    public static readonly DevelopmentPersonaDefinition RoomOperationsInspector = new(
        "roomops.inspector@localhost",
        SystemRoleTemplates.RoomInspector,
        [RoomOperationsPermissions.Read, RoomOperationsPermissions.Inspect],
        DevelopmentWorkforceSeeder.AnkaraPropertyId);

    public static readonly DevelopmentPersonaDefinition RoomOperationsManager = new(
        "roomops.manager@localhost",
        SystemRoleTemplates.RoomOperationsManager,
        [RoomOperationsPermissions.Read, RoomOperationsPermissions.Manage, RoomOperationsPermissions.Inspect],
        DevelopmentWorkforceSeeder.AnkaraPropertyId);

    public static readonly DevelopmentPersonaDefinition MaintenanceTechnician = new(
        "maintenance.technician@localhost",
        SystemRoleTemplates.MaintenanceTechnician,
        [MaintenancePermissions.Read, MaintenancePermissions.Resolve],
        DevelopmentWorkforceSeeder.AnkaraPropertyId);

    public static readonly DevelopmentPersonaDefinition MaintenanceManager = new(
        "maintenance.manager@localhost",
        SystemRoleTemplates.MaintenanceManager,
        [MaintenancePermissions.Read, MaintenancePermissions.Manage, MaintenancePermissions.Resolve],
        DevelopmentWorkforceSeeder.AnkaraPropertyId);

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
