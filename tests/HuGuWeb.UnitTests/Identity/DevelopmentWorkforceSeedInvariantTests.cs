using HuGuWeb.Api.Authorization;
using HuGuWeb.Api.Identity;
using HuGuWeb.Workforce.Domain;
using HuGuWeb.Workforce.Infrastructure.Seeding;

namespace HuGuWeb.UnitTests.Identity;

public class DevelopmentWorkforceSeedInvariantTests
{
    [Fact]
    public void EveryPersonaEmployeeFixture_HasMatchingIdentityCapableAccount()
    {
        Assert.Equal(
            DevelopmentPersonaEmployeeFixtures.All.Count,
            DevelopmentPersonaCatalog.AdditionalPersonas.Count(item => item.LinkedEmployeeId.HasValue));

        foreach (var fixture in DevelopmentPersonaEmployeeFixtures.All)
        {
            var persona = DevelopmentPersonaCatalog.AdditionalPersonas.Single(item =>
                item.Email.Equals(fixture.PersonaEmail, StringComparison.OrdinalIgnoreCase));
            Assert.Equal(fixture.EmployeeId, persona.LinkedEmployeeId);
            Assert.Equal(fixture.AccountLinkId, persona.LinkedAccountLinkId);
            Assert.NotNull(persona.MembershipId);
            Assert.Contains(SystemRoleTemplates.EmployeeLeaveSelfService, persona.AssignedRoleCodes);
            Assert.False(string.IsNullOrWhiteSpace(fixture.PersonnelNumber));
        }
    }

    [Fact]
    public void IdentityLinksAndMemberships_AreUnique()
    {
        var employees = DevelopmentPersonaCatalog.AdditionalPersonas
            .Where(item => item.LinkedEmployeeId.HasValue)
            .Select(item => item.LinkedEmployeeId!.Value)
            .ToList();
        var links = DevelopmentPersonaCatalog.AdditionalPersonas
            .Where(item => item.LinkedAccountLinkId.HasValue)
            .Select(item => item.LinkedAccountLinkId!.Value)
            .ToList();
        var memberships = DevelopmentPersonaCatalog.AdditionalPersonas
            .Where(item => item.MembershipId.HasValue)
            .Select(item => item.MembershipId!.Value)
            .ToList();

        Assert.Equal(employees.Count, employees.Distinct().Count());
        Assert.Equal(links.Count, links.Distinct().Count());
        Assert.Equal(memberships.Count, memberships.Distinct().Count());
        Assert.Equal(employees.Count, links.Count);
    }

    [Fact]
    public void ExistingHrOperatorEmails_ArePreserved()
    {
        Assert.Equal("hr.manager@localhost", DevelopmentPersonaCatalog.HumanResourcesManager.Email);
        Assert.Equal("hr.antalya@localhost", DevelopmentPersonaCatalog.HotelBHumanResourcesManager.Email);
        Assert.Equal("hr.corporate@localhost", DevelopmentPersonaCatalog.CorporateHumanResources.Email);
        Assert.Equal(SystemRoleTemplates.HrManager, DevelopmentPersonaCatalog.HumanResourcesManager.RoleCode);
        Assert.Equal(SystemRoleTemplates.HrManager, DevelopmentPersonaCatalog.HotelBHumanResourcesManager.RoleCode);
        Assert.Equal(SystemRoleTemplates.CorporateHr, DevelopmentPersonaCatalog.CorporateHumanResources.RoleCode);
        Assert.Null(DevelopmentPersonaCatalog.CorporateHumanResources.LinkedEmployeeId);
        Assert.Null(DevelopmentPersonaCatalog.Broad(null).LinkedEmployeeId);
    }

    [Fact]
    public void LoginEmailConvention_RemainsLocalhostDevelopmentAccounts()
    {
        Assert.All(
            DevelopmentPersonaCatalog.AdditionalPersonas,
            persona => Assert.EndsWith("@localhost", persona.Email, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            DevelopmentPersonaCatalog.AdditionalPersonas,
            persona => persona.Email.Contains("demo.hugu", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ManagerPersonas_UseReportingLinesAndRoles_NotPositionText()
    {
        Assert.Contains(
            DevelopmentPersonaEmployeeFixtures.ReportingLines,
            item => item.ManagerEmploymentId == DevelopmentPersonaEmployeeFixtures.RoomOpsManagerEmploymentId
                && item.SubordinateEmploymentId == DevelopmentPersonaEmployeeFixtures.RoomAttendantEmploymentId);
        Assert.Contains(
            DevelopmentPersonaEmployeeFixtures.ReportingLines,
            item => item.ManagerEmploymentId == DevelopmentPersonaEmployeeFixtures.MaintenanceManagerEmploymentId
                && item.SubordinateEmploymentId == DevelopmentPersonaEmployeeFixtures.MaintenanceTechnicianEmploymentId);

        Assert.All(
            DevelopmentPersonaEmployeeFixtures.ReportingLines,
            line =>
            {
                Assert.NotEqual(line.SubordinateEmploymentId, line.ManagerEmploymentId);
                var subordinate = DevelopmentPersonaEmployeeFixtures.All.Single(item =>
                    item.EmploymentId == line.SubordinateEmploymentId);
                var manager = DevelopmentPersonaEmployeeFixtures.All.Single(item =>
                    item.EmploymentId == line.ManagerEmploymentId);
                Assert.Equal(subordinate.DepartmentCode, manager.DepartmentCode);
            });

        Assert.Equal("HK-SUP", DevelopmentPersonaEmployeeFixtures.All.Single(item =>
            item.PersonnelNumber == "DEMO-HK-MGR-01").PositionCode);
        Assert.Equal("HK-SUP", DevelopmentPersonaEmployeeFixtures.All.Single(item =>
            item.PersonnelNumber == "DEMO-HK-INS-01").PositionCode);
        Assert.Equal(["HK"], DevelopmentPersonaCatalog.RoomOperationsManager.DepartmentScopeCodes);
        Assert.Equal(["ENG"], DevelopmentPersonaCatalog.MaintenanceManager.DepartmentScopeCodes);
        Assert.Null(typeof(Position).GetProperty("Grade"));
        Assert.Null(typeof(Position).GetProperty("Level"));
        Assert.Null(typeof(Position).GetProperty("Rank"));
    }

    [Fact]
    public void ApplicabilityMaps_AreCodeBasedAndPropertyAgnostic()
    {
        var ankaraHr = CreateDepartment(DevelopmentWorkforceSeeder.HumanResourcesDepartmentId, DevelopmentWorkforceSeeder.AnkaraPropertyId, "HR");
        var ankaraHk = CreateDepartment(DevelopmentWorkforceSeeder.HousekeepingDepartmentId, DevelopmentWorkforceSeeder.AnkaraPropertyId, "HK");
        var ankaraFo = CreateDepartment(DevelopmentWorkforceSeeder.FrontOfficeDepartmentId, DevelopmentWorkforceSeeder.AnkaraPropertyId, "FO");
        var ankaraEng = CreateDepartment(DevelopmentWorkforceSeeder.TechnicalDepartmentId, DevelopmentWorkforceSeeder.AnkaraPropertyId, "ENG");
        var ankaraFnb = CreateDepartment(DevelopmentWorkforceSeeder.FoodBeverageDepartmentId, DevelopmentWorkforceSeeder.AnkaraPropertyId, "FNB");
        var antalyaHr = CreateDepartment(DevelopmentWorkplaceCatalog.AntalyaHumanResourcesDepartmentId, DevelopmentWorkforceSeeder.AntalyaPropertyId, "HR");
        var antalyaHk = CreateDepartment(DevelopmentWorkplaceCatalog.AntalyaHousekeepingDepartmentId, DevelopmentWorkforceSeeder.AntalyaPropertyId, "HK");
        var antalyaFo = CreateDepartment(DevelopmentWorkplaceCatalog.AntalyaFrontOfficeDepartmentId, DevelopmentWorkforceSeeder.AntalyaPropertyId, "FO");
        var antalyaEng = CreateDepartment(DevelopmentWorkplaceCatalog.AntalyaTechnicalDepartmentId, DevelopmentWorkforceSeeder.AntalyaPropertyId, "ENG");
        var antalyaFnb = CreateDepartment(DevelopmentWorkplaceCatalog.AntalyaFoodBeverageDepartmentId, DevelopmentWorkforceSeeder.AntalyaPropertyId, "FNB");

        var ankaraPositions = PositionsFor(DevelopmentWorkforceSeeder.AnkaraPropertyId);
        var antalyaPositions = PositionsFor(DevelopmentWorkforceSeeder.AntalyaPropertyId);

        var first = DevelopmentWorkplaceCatalog.ResolveApplicability(
            [ankaraHr, ankaraHk, ankaraFo, ankaraEng, ankaraFnb, antalyaHr, antalyaHk, antalyaFo, antalyaEng, antalyaFnb],
            [..ankaraPositions, ..antalyaPositions]);
        var second = DevelopmentWorkplaceCatalog.ResolveApplicability(
            [ankaraHr, ankaraHk, ankaraFo, ankaraEng, ankaraFnb, antalyaHr, antalyaHk, antalyaFo, antalyaEng, antalyaFnb],
            [..ankaraPositions, ..antalyaPositions]);

        Assert.Equal(first.Count, second.Count);
        Assert.Equal(first.ToHashSet(), second.ToHashSet());
        Assert.Contains((ankaraHk.Id, ankaraPositions.Single(item => item.Code == "HK-ATT").Id), first);
        Assert.Contains((antalyaHk.Id, antalyaPositions.Single(item => item.Code == "HK-ATT").Id), first);
        Assert.Contains((antalyaHr.Id, antalyaPositions.Single(item => item.Code == "HR-OFF").Id), first);
        Assert.Contains((antalyaFo.Id, antalyaPositions.Single(item => item.Code == "FO-REC").Id), first);
        Assert.Contains((antalyaEng.Id, antalyaPositions.Single(item => item.Code == "ENG-TECH").Id), first);
        Assert.DoesNotContain(
            first,
            pair => pair.DepartmentId == ankaraHk.Id && pair.PositionId == antalyaPositions.Single(item => item.Code == "HK-ATT").Id);
        Assert.Equal(5, DevelopmentWorkplaceCatalog.DepartmentsFor(DevelopmentWorkforceSeeder.AnkaraPropertyId).Count);
        Assert.Equal(
            DevelopmentWorkplaceCatalog.DepartmentsFor(DevelopmentWorkforceSeeder.AnkaraPropertyId).Select(item => item.Code).OrderBy(item => item, StringComparer.Ordinal),
            DevelopmentWorkplaceCatalog.DepartmentsFor(DevelopmentWorkforceSeeder.AntalyaPropertyId).Select(item => item.Code).OrderBy(item => item, StringComparer.Ordinal));
        Assert.Equal(
            DevelopmentWorkplaceCatalog.Positions.Count,
            DevelopmentWorkplaceCatalog.Positions.Select(item => item.Code).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void FixtureAssignments_StayInsideOneProperty()
    {
        foreach (var fixture in DevelopmentPersonaEmployeeFixtures.All)
        {
            var departments = DevelopmentWorkplaceCatalog.DepartmentsFor(fixture.PropertyId);
            Assert.Contains(departments, item => item.Code == fixture.DepartmentCode);
            Assert.Contains(DevelopmentWorkplaceCatalog.Positions, item => item.Code == fixture.PositionCode);
            Assert.Equal(
                departments.Single(item => item.Code == fixture.DepartmentCode).Id,
                fixture.PreferredDepartmentId);
        }
    }

    [Fact]
    public void StandardEmployee_DoesNotReceiveHrAdminPermissions()
    {
        var leastPrivilege = DevelopmentPersonaCatalog.FrontOfficeReceptionist.Permissions
            .Concat(DevelopmentPersonaCatalog.RoomOperationsAttendant.Permissions)
            .Concat(DevelopmentPersonaCatalog.MaintenanceTechnician.Permissions);

        Assert.DoesNotContain(HrEmployeePermissions.Manage, leastPrivilege);
        Assert.DoesNotContain(HrMovementPermissions.Manage, leastPrivilege);
        Assert.DoesNotContain(AuthorizationPermissions.RolesManage, leastPrivilege);
        Assert.Contains(HrLeavePermissions.Request, DevelopmentPersonaCatalog.FrontOfficeReceptionist.Permissions);
        Assert.Contains(HrLeavePermissions.Request, DevelopmentPersonaCatalog.MaintenanceTechnician.Permissions);
    }

    [Fact]
    public void ApplicabilitySeedSource_DoesNotHardCodeAnkaraPropertyFilter()
    {
        var seederPath = Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "modules",
            "HuGuWeb.Workforce.Infrastructure",
            "Seeding",
            "DevelopmentWorkforceSeeder.cs");
        var source = File.ReadAllText(seederPath);
        var methodStart = source.IndexOf("private static async Task TrySeedPositionApplicabilityAsync", StringComparison.Ordinal);
        Assert.True(methodStart > 0);
        var methodEnd = source.IndexOf("private static void AddPosition", methodStart, StringComparison.Ordinal);
        Assert.True(methodEnd > methodStart);
        var method = source[methodStart..methodEnd];
        Assert.DoesNotContain("item.PropertyId == PropertyId", method, StringComparison.Ordinal);
        Assert.Contains("item.OrganizationId == OrganizationId", method, StringComparison.Ordinal);
        Assert.Contains("DevelopmentWorkplaceCatalog.ResolveApplicability", method, StringComparison.Ordinal);
    }

    private static Department CreateDepartment(Guid id, Guid propertyId, string code)
    {
        Assert.True(Department.TryCreate(id, propertyId, code, code, out var department, out var error), error);
        return department!;
    }

    private static List<Position> PositionsFor(Guid propertyId)
    {
        var list = new List<Position>();
        foreach (var (name, code) in DevelopmentWorkplaceCatalog.Positions)
        {
            Assert.True(Position.TryCreate(Guid.CreateVersion7(), propertyId, name, code, out var position, out var error), error);
            list.Add(position!);
        }

        return list;
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "HuGuWeb.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
