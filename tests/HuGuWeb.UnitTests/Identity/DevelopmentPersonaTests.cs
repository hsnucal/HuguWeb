using HuGuWeb.Api.Authorization;
using HuGuWeb.Api.Identity;

namespace HuGuWeb.UnitTests.Identity;

public class DevelopmentPersonaTests
{
    [Fact]
    public void BroadPersona_HasWorkforceAndRoomOperationsPermissions()
    {
        var persona = DevelopmentPersonaCatalog.Broad(null);

        Assert.Equal("dev@localhost", persona.Email);
        Assert.Equal(DevelopmentPersonaCatalog.BroadPermissions, persona.Permissions);
        Assert.Contains(WorkforcePermissions.Read, persona.Permissions);
        Assert.Contains(WorkforcePermissions.Manage, persona.Permissions);
        Assert.Contains(RoomOperationsPermissions.Read, persona.Permissions);
        Assert.Contains(RoomOperationsPermissions.Manage, persona.Permissions);
        Assert.Contains(RoomOperationsPermissions.Inspect, persona.Permissions);
        Assert.Contains(MaintenancePermissions.Read, persona.Permissions);
        Assert.Contains(MaintenancePermissions.Manage, persona.Permissions);
        Assert.Contains(MaintenancePermissions.Resolve, persona.Permissions);
        Assert.Contains(HrEmployeePermissions.Read, persona.Permissions);
        Assert.Contains(HrEmployeePermissions.Manage, persona.Permissions);
        Assert.Contains(HrEmployeePermissions.SensitiveRead, persona.Permissions);
    }

    [Fact]
    public void HumanResourcesManager_HasWorkforceAndHrEmployeePermissions()
    {
        var persona = DevelopmentPersonaCatalog.HumanResourcesManager;

        Assert.Equal("hr.manager@localhost", persona.Email);
        Assert.Equal(
            [
                WorkforcePermissions.Read,
                WorkforcePermissions.Manage,
                HrEmployeePermissions.Read,
                HrEmployeePermissions.Manage,
                HrEmployeePermissions.SensitiveRead
            ],
            persona.Permissions);
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("room-operations.", StringComparison.Ordinal));
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("maintenance.", StringComparison.Ordinal));
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("hr.official.", StringComparison.Ordinal));
    }

    [Fact]
    public void RoomOperationsAttendant_CanCleanButNotInspectOrManageWorkforce()
    {
        var persona = DevelopmentPersonaCatalog.RoomOperationsAttendant;

        Assert.Equal("roomops.attendant@localhost", persona.Email);
        Assert.Equal([RoomOperationsPermissions.Read, RoomOperationsPermissions.Manage], persona.Permissions);
        Assert.DoesNotContain(RoomOperationsPermissions.Inspect, persona.Permissions);
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("workforce.", StringComparison.Ordinal));
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("hr.employee.", StringComparison.Ordinal));
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("hr.official.", StringComparison.Ordinal));
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("maintenance.", StringComparison.Ordinal));
    }

    [Fact]
    public void RoomOperationsInspector_CanInspectButNotManageCleaningOrWorkforce()
    {
        var persona = DevelopmentPersonaCatalog.RoomOperationsInspector;

        Assert.Equal("roomops.inspector@localhost", persona.Email);
        Assert.Equal([RoomOperationsPermissions.Read, RoomOperationsPermissions.Inspect], persona.Permissions);
        Assert.DoesNotContain(RoomOperationsPermissions.Manage, persona.Permissions);
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("workforce.", StringComparison.Ordinal));
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("hr.employee.", StringComparison.Ordinal));
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("hr.official.", StringComparison.Ordinal));
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("maintenance.", StringComparison.Ordinal));
    }

    [Fact]
    public void RoomOperationsManager_HasFullRoomOperations_NoWorkforce()
    {
        var persona = DevelopmentPersonaCatalog.RoomOperationsManager;

        Assert.Equal("roomops.manager@localhost", persona.Email);
        Assert.Equal(
            [RoomOperationsPermissions.Read, RoomOperationsPermissions.Manage, RoomOperationsPermissions.Inspect],
            persona.Permissions);
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("workforce.", StringComparison.Ordinal));
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("hr.employee.", StringComparison.Ordinal));
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("hr.official.", StringComparison.Ordinal));
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("maintenance.", StringComparison.Ordinal));
    }

    [Fact]
    public void MaintenanceTechnician_HasReadAndResolve_NoManageOrOtherDomains()
    {
        var persona = DevelopmentPersonaCatalog.MaintenanceTechnician;

        Assert.Equal("maintenance.technician@localhost", persona.Email);
        Assert.Equal([MaintenancePermissions.Read, MaintenancePermissions.Resolve], persona.Permissions);
        Assert.DoesNotContain(MaintenancePermissions.Manage, persona.Permissions);
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("workforce.", StringComparison.Ordinal));
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("hr.employee.", StringComparison.Ordinal));
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("hr.official.", StringComparison.Ordinal));
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("room-operations.", StringComparison.Ordinal));
    }

    [Fact]
    public void MaintenanceManager_HasFullMaintenance_NoWorkforceOrRoomOperations()
    {
        var persona = DevelopmentPersonaCatalog.MaintenanceManager;

        Assert.Equal("maintenance.manager@localhost", persona.Email);
        Assert.Equal(
            [MaintenancePermissions.Read, MaintenancePermissions.Manage, MaintenancePermissions.Resolve],
            persona.Permissions);
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("workforce.", StringComparison.Ordinal));
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("hr.employee.", StringComparison.Ordinal));
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("hr.official.", StringComparison.Ordinal));
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("room-operations.", StringComparison.Ordinal));
    }

    [Fact]
    public void Catalog_DoesNotAddHrOfficialPermissionFamily()
    {
        var all = DevelopmentPersonaCatalog.Broad(null).Permissions
            .Concat(DevelopmentPersonaCatalog.AdditionalPersonas.SelectMany(item => item.Permissions));
        Assert.DoesNotContain(all, value => value.StartsWith("hr.official.", StringComparison.Ordinal));
        Assert.Contains(HrEmployeePermissions.Read, DevelopmentPersonaCatalog.HumanResourcesManager.Permissions);
        Assert.Contains(HrEmployeePermissions.Manage, DevelopmentPersonaCatalog.HumanResourcesManager.Permissions);
        Assert.Contains(WorkforcePermissions.Manage, DevelopmentPersonaCatalog.HumanResourcesManager.Permissions);
        Assert.DoesNotContain(
            HrEmployeePermissions.Read,
            DevelopmentPersonaCatalog.RoomOperationsManager.Permissions);
        Assert.DoesNotContain(
            HrEmployeePermissions.Read,
            DevelopmentPersonaCatalog.MaintenanceManager.Permissions);
    }

    [Fact]
    public void AdditionalPersonas_DoNotIncludeHrSpecialist()
    {
        Assert.DoesNotContain(
            DevelopmentPersonaCatalog.AdditionalPersonas,
            persona => persona.Email.Equals("hr.specialist@localhost", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Convergence_AddsMissingAndRemovesStaleDevelopmentPermissionsOnly()
    {
        var current = new[]
        {
            WorkforcePermissions.Read,
            RoomOperationsPermissions.Inspect,
            "custom.unrelated"
        };

        var (add, remove) = DevelopmentPermissionConvergence.Diff(
            current,
            DevelopmentPersonaCatalog.HumanResourcesManager.Permissions);

        Assert.Equal(
            [
                HrEmployeePermissions.Manage,
                HrEmployeePermissions.Read,
                HrEmployeePermissions.SensitiveRead,
                WorkforcePermissions.Manage
            ],
            add);
        Assert.Equal([RoomOperationsPermissions.Inspect], remove);
    }

    [Fact]
    public void Catalog_DoesNotUsePositionNames()
    {
        var emails = string.Join(' ', DevelopmentPersonaCatalog.AdditionalPersonas.Select(item => item.Email));
        Assert.DoesNotContain("Kat", emails, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Supervisor", emails, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Housekeeping", emails, StringComparison.OrdinalIgnoreCase);
    }
}
