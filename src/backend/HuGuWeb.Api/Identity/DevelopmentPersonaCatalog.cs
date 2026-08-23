using HuGuWeb.Api.Authorization;

namespace HuGuWeb.Api.Identity;

public sealed record DevelopmentPersonaDefinition(string Email, IReadOnlyList<string> Permissions);

public static class DevelopmentPersonaCatalog
{
    public const string BroadEmailKey = "DevelopmentUser:Email";
    public const string BroadPasswordKey = "DevelopmentUser:Password";
    public const string DefaultPasswordKey = "DevelopmentUsers:DefaultPassword";
    public const string DefaultBroadEmail = "dev@localhost";

    public static readonly IReadOnlyList<string> AllDevelopmentPermissions =
    [
        WorkforcePermissions.Read,
        WorkforcePermissions.Manage,
        RoomOperationsPermissions.Read,
        RoomOperationsPermissions.Manage,
        RoomOperationsPermissions.Inspect,
        MaintenancePermissions.Read,
        MaintenancePermissions.Manage,
        MaintenancePermissions.Resolve
    ];

    public static readonly IReadOnlyList<string> BroadPermissions = AllDevelopmentPermissions;

    public static readonly DevelopmentPersonaDefinition HumanResourcesManager = new(
        "hr.manager@localhost",
        [WorkforcePermissions.Read, WorkforcePermissions.Manage]);

    public static readonly DevelopmentPersonaDefinition RoomOperationsAttendant = new(
        "roomops.attendant@localhost",
        [RoomOperationsPermissions.Read, RoomOperationsPermissions.Manage]);

    public static readonly DevelopmentPersonaDefinition RoomOperationsInspector = new(
        "roomops.inspector@localhost",
        [RoomOperationsPermissions.Read, RoomOperationsPermissions.Inspect]);

    public static readonly DevelopmentPersonaDefinition RoomOperationsManager = new(
        "roomops.manager@localhost",
        [RoomOperationsPermissions.Read, RoomOperationsPermissions.Manage, RoomOperationsPermissions.Inspect]);

    public static readonly DevelopmentPersonaDefinition MaintenanceTechnician = new(
        "maintenance.technician@localhost",
        [MaintenancePermissions.Read, MaintenancePermissions.Resolve]);

    public static readonly DevelopmentPersonaDefinition MaintenanceManager = new(
        "maintenance.manager@localhost",
        [MaintenancePermissions.Read, MaintenancePermissions.Manage, MaintenancePermissions.Resolve]);

    public static IReadOnlyList<DevelopmentPersonaDefinition> AdditionalPersonas { get; } =
    [
        HumanResourcesManager,
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
        return new DevelopmentPersonaDefinition(email, BroadPermissions);
    }
}

public static class DevelopmentPermissionConvergence
{
    public static (IReadOnlyList<string> Add, IReadOnlyList<string> Remove) Diff(
        IEnumerable<string> currentPermissionValues,
        IReadOnlyCollection<string> expectedPermissions)
    {
        var current = currentPermissionValues
            .Where(value => DevelopmentPersonaCatalog.AllDevelopmentPermissions.Contains(value))
            .ToHashSet(StringComparer.Ordinal);
        var expected = expectedPermissions.ToHashSet(StringComparer.Ordinal);

        return (
            expected.Except(current, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            current.Except(expected, StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
    }
}
