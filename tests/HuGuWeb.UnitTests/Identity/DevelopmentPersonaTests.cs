using HuGuWeb.Api.Authorization;
using HuGuWeb.Api.Identity;
using HuGuWeb.Workforce.Infrastructure.Seeding;

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
        Assert.Contains(AuthorizationPermissions.UsersManage, persona.Permissions);
        Assert.Contains(AuthorizationPermissions.RolesManage, persona.Permissions);
        Assert.Equal(SystemRoleTemplates.DevelopmentSuperuser, persona.RoleCode);
        Assert.Null(persona.PropertyId);
    }

    [Fact]
    public void HumanResourcesManager_HasWorkforceAndHrEmployeePermissions()
    {
        var persona = DevelopmentPersonaCatalog.HumanResourcesManager;

        Assert.Equal("hr.manager@localhost", persona.Email);
        Assert.Equal(SystemRoleTemplates.HrManager, persona.RoleCode);
        Assert.Equal(SystemRoleTemplates.HumanResourcesPermissions, persona.Permissions);
        Assert.Contains(SystemRoleTemplates.EmployeeLeaveSelfService, persona.AssignedRoleCodes);
        Assert.Equal(DevelopmentPersonaEmployeeFixtures.HrManagerEmployeeId, persona.LinkedEmployeeId);
        Assert.Equal(DevelopmentPersonaEmployeeFixtures.HrManagerLinkId, persona.LinkedAccountLinkId);
        Assert.Contains(HrLeavePermissions.Read, persona.Permissions);
        Assert.Contains(HrLeavePermissions.Manage, persona.Permissions);
        Assert.DoesNotContain(HrLeavePermissions.Request, persona.Permissions);
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("room-operations.", StringComparison.Ordinal));
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("maintenance.", StringComparison.Ordinal));
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("hr.official.", StringComparison.Ordinal));
        Assert.Equal(DevelopmentWorkforceSeeder.AnkaraPropertyId, persona.PropertyId);
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
    public void MaintenanceTechnician_HasMaintenanceResolveAndLeaveSelfService()
    {
        var persona = DevelopmentPersonaCatalog.MaintenanceTechnician;

        Assert.Equal("maintenance.technician@localhost", persona.Email);
        Assert.Equal(SystemRoleTemplates.MaintenanceTechnician, persona.RoleCode);
        Assert.Contains(SystemRoleTemplates.EmployeeLeaveSelfService, persona.AssignedRoleCodes);
        Assert.Equal(DevelopmentPersonaEmployeeFixtures.MaintenanceTechnicianEmployeeId, persona.LinkedEmployeeId);
        Assert.Equal(DevelopmentPersonaEmployeeFixtures.MaintenanceTechnicianLinkId, persona.LinkedAccountLinkId);
        Assert.Contains(MaintenancePermissions.Read, persona.Permissions);
        Assert.Contains(MaintenancePermissions.Resolve, persona.Permissions);
        Assert.Contains(HrLeavePermissions.Request, persona.Permissions);
        Assert.DoesNotContain(MaintenancePermissions.Manage, persona.Permissions);
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("workforce.", StringComparison.Ordinal));
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("hr.employee.", StringComparison.Ordinal));
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("hr.official.", StringComparison.Ordinal));
        Assert.DoesNotContain(persona.Permissions, value => value.StartsWith("room-operations.", StringComparison.Ordinal));
    }

    [Fact]
    public void EmployeePersonas_HaveDistinctLinkedEmployees()
    {
        var linked = DevelopmentPersonaCatalog.AdditionalPersonas
            .Where(item => item.LinkedEmployeeId.HasValue)
            .Select(item => item.LinkedEmployeeId!.Value)
            .ToList();

        Assert.Equal(linked.Count, linked.Distinct().Count());
        Assert.NotEqual(
            DevelopmentPersonaEmployeeFixtures.MaintenanceTechnicianEmployeeId,
            DevelopmentPersonaEmployeeFixtures.HrManagerEmployeeId);
        Assert.DoesNotContain(
            DevelopmentWorkforceSeeder.DevelopmentEmployeeId,
            linked);
    }

    [Fact]
    public void NonEmployeePersonas_HaveNoEmployeeAccountLink()
    {
        Assert.Null(DevelopmentPersonaCatalog.Broad(null).LinkedEmployeeId);
        Assert.Null(DevelopmentPersonaCatalog.CorporateHumanResources.LinkedEmployeeId);
    }

    [Fact]
    public void PersonaEmployeeFixtures_MatchCatalogLinks()
    {
        foreach (var fixture in DevelopmentPersonaEmployeeFixtures.All)
        {
            var persona = DevelopmentPersonaCatalog.AdditionalPersonas.Single(item =>
                item.Email.Equals(fixture.PersonaEmail, StringComparison.OrdinalIgnoreCase));
            Assert.Equal(fixture.EmployeeId, persona.LinkedEmployeeId);
            Assert.Equal(fixture.AccountLinkId, persona.LinkedAccountLinkId);
        }
    }

    [Fact]
    public void MaintenanceManager_HasFullMaintenance_AndDepartmentLeaveApprover()
    {
        var persona = DevelopmentPersonaCatalog.MaintenanceManager;

        Assert.Equal("maintenance.manager@localhost", persona.Email);
        Assert.Equal(SystemRoleTemplates.MaintenanceManager, persona.RoleCode);
        Assert.Contains(SystemRoleTemplates.DepartmentLeaveApprover, persona.AssignedRoleCodes);
        Assert.Contains(SystemRoleTemplates.DepartmentScheduler, persona.AssignedRoleCodes);
        Assert.Contains(SystemRoleTemplates.EmployeeLeaveSelfService, persona.AssignedRoleCodes);
        Assert.Equal(["ENG"], persona.DepartmentScopeCodes);
        Assert.Contains(MaintenancePermissions.Read, persona.Permissions);
        Assert.Contains(MaintenancePermissions.Manage, persona.Permissions);
        Assert.Contains(MaintenancePermissions.Resolve, persona.Permissions);
        Assert.Contains(HrLeavePermissions.Read, persona.Permissions);
        Assert.Contains(HrLeavePermissions.Approve, persona.Permissions);
        Assert.Contains(HrSchedulePermissions.Read, persona.Permissions);
        Assert.Contains(HrSchedulePermissions.Manage, persona.Permissions);
        Assert.Contains(HrShiftDefinitionPermissions.Read, persona.Permissions);
        Assert.DoesNotContain(HrLeavePermissions.Manage, persona.Permissions);
        Assert.DoesNotContain(HrShiftDefinitionPermissions.Manage, persona.Permissions);
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
    public void Personas_MapToDatabaseRoleCodes_NotRuntimeEmailChecks()
    {
        Assert.Equal(SystemRoleTemplates.RoomAttendant, DevelopmentPersonaCatalog.RoomOperationsAttendant.RoleCode);
        Assert.Equal(SystemRoleTemplates.RoomInspector, DevelopmentPersonaCatalog.RoomOperationsInspector.RoleCode);
        Assert.Equal(SystemRoleTemplates.RoomOperationsManager, DevelopmentPersonaCatalog.RoomOperationsManager.RoleCode);
        Assert.Equal(SystemRoleTemplates.MaintenanceTechnician, DevelopmentPersonaCatalog.MaintenanceTechnician.RoleCode);
        Assert.Equal(SystemRoleTemplates.MaintenanceManager, DevelopmentPersonaCatalog.MaintenanceManager.RoleCode);
        Assert.Equal(
            SystemRoleTemplates.ByCode(DevelopmentPersonaCatalog.HumanResourcesManager.RoleCode)!.Permissions,
            DevelopmentPersonaCatalog.HumanResourcesManager.Permissions);
    }

    [Fact]
    public void Catalog_DoesNotUsePositionNames()
    {
        var emails = string.Join(' ', DevelopmentPersonaCatalog.AdditionalPersonas.Select(item => item.Email));
        Assert.DoesNotContain("Kat", emails, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Supervisor", emails, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Housekeeping", emails, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MultiPropertyPersonas_UseExplicitPropertyOrOrganizationScope()
    {
        Assert.Equal(
            DevelopmentWorkforceSeeder.AntalyaPropertyId,
            DevelopmentPersonaCatalog.HotelBHumanResourcesManager.PropertyId);
        Assert.Equal("hr.antalya@localhost", DevelopmentPersonaCatalog.HotelBHumanResourcesManager.Email);
        Assert.Null(DevelopmentPersonaCatalog.CorporateHumanResources.PropertyId);
        Assert.Equal("hr.corporate@localhost", DevelopmentPersonaCatalog.CorporateHumanResources.Email);
        Assert.Equal(SystemRoleTemplates.CorporateHr, DevelopmentPersonaCatalog.CorporateHumanResources.RoleCode);
    }
}
