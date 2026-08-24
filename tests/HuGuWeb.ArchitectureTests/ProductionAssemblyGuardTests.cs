using HuGuWeb.Api.Authorization;
using HuGuWeb.Api.Identity;
using HuGuWeb.RoomOperations.Application;
using HuGuWeb.RoomOperations.Domain;
using HuGuWeb.RoomOperations.Infrastructure.Persistence;
using HuGuWeb.TechnicalService.Domain;
using HuGuWeb.TechnicalService.Infrastructure;
using HuGuWeb.TechnicalService.Infrastructure.Persistence;
using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;
using HuGuWeb.Workforce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HuGuWeb.ArchitectureTests;

public class ProductionAssemblyGuardTests
{
    [Fact]
    public void Api_DoesNotReference_TestAssemblies()
    {
        var referencedNames = GetReferencedAssemblyNames(typeof(AppIdentityDbContext).Assembly);

        Assert.DoesNotContain(referencedNames, name =>
            name.Contains("Test", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Api_DoesNotReference_RejectedBootstrapDependencies()
    {
        AssertNoRejectedInfrastructure(typeof(AppIdentityDbContext).Assembly);
    }

    [Fact]
    public void IdentityContext_DoesNotExpose_HotelDomainSets()
    {
        var dbSetProperties = typeof(AppIdentityDbContext)
            .GetProperties()
            .Where(property => property.PropertyType.IsGenericType
                && property.PropertyType.Name.StartsWith("DbSet", StringComparison.Ordinal))
            .Select(property => property.Name)
            .ToArray();

        string[] forbidden =
        [
            "Hotels",
            "Properties",
            "Tenants",
            "Rooms",
            "Reservations",
            "Guests",
            "Housekeeping",
            "Folios",
            "Employees",
            "Departments",
            "Positions",
            "Employments",
            "Assignments",
            "Organizations",
            "MaintenanceIssues",
            "MaintenanceIssueCategories"
        ];

        foreach (var name in forbidden)
        {
            Assert.DoesNotContain(name, dbSetProperties);
        }
    }

    [Fact]
    public void WorkforceDomain_DoesNotDependOn_ApiHost()
    {
        var referencedNames = GetReferencedAssemblyNames(typeof(Employee).Assembly);

        Assert.DoesNotContain("HuGuWeb.Api", referencedNames);
        Assert.DoesNotContain("Microsoft.AspNetCore.Identity.EntityFrameworkCore", referencedNames);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", referencedNames);
        Assert.DoesNotContain("Npgsql.EntityFrameworkCore.PostgreSQL", referencedNames);
        AssertNoRejectedInfrastructure(typeof(Employee).Assembly);
    }

    [Fact]
    public void WorkforceInfrastructure_DoesNotDependOn_ApiHostOrRejectedLibraries()
    {
        var referencedNames = GetReferencedAssemblyNames(typeof(WorkforceDbContext).Assembly);

        Assert.DoesNotContain("HuGuWeb.Api", referencedNames);
        AssertNoRejectedInfrastructure(typeof(WorkforceDbContext).Assembly);
    }

    [Fact]
    public void RoomOperationsDomain_DoesNotDependOn_ApiHostOrEf()
    {
        var referencedNames = GetReferencedAssemblyNames(typeof(Room).Assembly);

        Assert.DoesNotContain("HuGuWeb.Api", referencedNames);
        Assert.DoesNotContain("HuGuWeb.TechnicalService", referencedNames);
        Assert.DoesNotContain("HuGuWeb.TechnicalService.Infrastructure", referencedNames);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", referencedNames);
        Assert.DoesNotContain("Npgsql.EntityFrameworkCore.PostgreSQL", referencedNames);
        Assert.DoesNotContain("Microsoft.AspNetCore.Identity.EntityFrameworkCore", referencedNames);
        AssertNoRejectedInfrastructure(typeof(Room).Assembly);
        AssertNoDeferredDomains(typeof(Room).Assembly.GetTypes().Select(type => type.Name));
    }

    [Fact]
    public void RoomOperationsInfrastructure_DoesNotDependOn_ApiHostOrRejectedLibraries()
    {
        var referencedNames = GetReferencedAssemblyNames(typeof(RoomOperationsDbContext).Assembly);

        Assert.DoesNotContain("HuGuWeb.Api", referencedNames);
        Assert.DoesNotContain("HuGuWeb.TechnicalService", referencedNames);
        Assert.DoesNotContain("HuGuWeb.TechnicalService.Infrastructure", referencedNames);
        AssertNoRejectedInfrastructure(typeof(RoomOperationsDbContext).Assembly);
        AssertNoDeferredDomains(typeof(RoomOperationsDbContext).Assembly.GetTypes().Select(type => type.Name));
    }

    [Fact]
    public void RoomOperations_DoesNotReference_TechnicalServiceAssemblies()
    {
        var domainNames = GetReferencedAssemblyNames(typeof(Room).Assembly);
        Assert.DoesNotContain("HuGuWeb.TechnicalService", domainNames);
        Assert.DoesNotContain("HuGuWeb.TechnicalService.Infrastructure", domainNames);
        Assert.True(typeof(IRoomServiceabilityLookup).IsAssignableFrom(typeof(TechnicalServiceRoomServiceabilityLookup)));
    }

    [Fact]
    public void RoomOperations_HasNoGenericRepositoryOrBroker()
    {
        var names = typeof(Room).Assembly.GetTypes()
            .Concat(typeof(RoomOperationsDbContext).Assembly.GetTypes())
            .Select(type => type.Name)
            .ToArray();

        Assert.DoesNotContain("IRepository", names);
        Assert.DoesNotContain("IGenericRepository", names);
        Assert.DoesNotContain("GenericRepository", names);
        Assert.DoesNotContain("IOutbox", names);
        Assert.DoesNotContain("OutboxMessage", names);
        Assert.DoesNotContain("IMessageBroker", names);
        AssertNoDeferredDomains(names);
    }

    [Fact]
    public void TechnicalServiceDomain_DoesNotDependOn_ApiHostEfOrSiblingImplementations()
    {
        var referencedNames = GetReferencedAssemblyNames(typeof(MaintenanceIssue).Assembly);

        Assert.DoesNotContain("HuGuWeb.Api", referencedNames);
        Assert.DoesNotContain("HuGuWeb.RoomOperations", referencedNames);
        Assert.DoesNotContain("HuGuWeb.RoomOperations.Infrastructure", referencedNames);
        Assert.DoesNotContain("HuGuWeb.Workforce", referencedNames);
        Assert.DoesNotContain("HuGuWeb.Workforce.Infrastructure", referencedNames);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", referencedNames);
        Assert.DoesNotContain("Npgsql.EntityFrameworkCore.PostgreSQL", referencedNames);
        Assert.DoesNotContain("Microsoft.AspNetCore.Identity.EntityFrameworkCore", referencedNames);
        AssertNoRejectedInfrastructure(typeof(MaintenanceIssue).Assembly);
    }

    [Fact]
    public void TechnicalServiceInfrastructure_DoesNotDependOn_ApiHostOrRejectedLibraries()
    {
        var referencedNames = GetReferencedAssemblyNames(typeof(TechnicalServiceDbContext).Assembly);

        Assert.DoesNotContain("HuGuWeb.Api", referencedNames);
        AssertNoRejectedInfrastructure(typeof(TechnicalServiceDbContext).Assembly);
    }

    [Fact]
    public void TechnicalService_HasNoGenericRepositoryOrBroker()
    {
        var names = typeof(MaintenanceIssue).Assembly.GetTypes()
            .Concat(typeof(TechnicalServiceDbContext).Assembly.GetTypes())
            .Select(type => type.Name)
            .ToArray();

        Assert.DoesNotContain("IRepository", names);
        Assert.DoesNotContain("IGenericRepository", names);
        Assert.DoesNotContain("GenericRepository", names);
        Assert.DoesNotContain("IOutbox", names);
        Assert.DoesNotContain("OutboxMessage", names);
        Assert.DoesNotContain("IMessageBroker", names);
        Assert.DoesNotContain("MaintenanceWorkOrder", names);
        Assert.DoesNotContain("IWorkflowEngine", names);
    }

    [Fact]
    public void TechnicalService_DoesNotAuthorizeByEmailPositionOrDepartmentName()
    {
        var names = typeof(MaintenanceIssue).Assembly.GetTypes()
            .Concat(typeof(TechnicalServiceDbContext).Assembly.GetTypes())
            .Concat(typeof(MaintenancePermissions).Assembly.GetTypes())
            .Select(type => type.FullName ?? type.Name)
            .ToArray();
        var joined = string.Join(' ', names);

        Assert.DoesNotContain("KatGorevlisi", joined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Teknisyen", joined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("IkMuduru", joined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TeknikServisUzmani", joined, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("maintenance.read", MaintenancePermissions.Read);
        Assert.Equal("maintenance.manage", MaintenancePermissions.Manage);
        Assert.Equal("maintenance.resolve", MaintenancePermissions.Resolve);
        Assert.Null(typeof(MaintenanceIssue).GetProperty("ApplicationUserId"));
        Assert.Null(typeof(MaintenanceIssue).GetProperty("UserId"));
    }

    [Fact]
    public void TechnicalService_DoesNotWriteRoomReadinessOrSellable()
    {
        Assert.Null(typeof(MaintenanceIssue).GetProperty("RoomReadiness"));
        Assert.Null(typeof(MaintenanceIssue).GetProperty("Sellable"));
        Assert.Null(typeof(MaintenanceIssue).GetProperty("RoomStatus"));
        Assert.Equal(["Dirty", "Clean", "Inspected", "Ready"], Enum.GetNames<RoomReadiness>());
        Assert.Equal(["Open", "InProgress", "UnableToResolve", "Resolved"], Enum.GetNames<MaintenanceIssueStatus>());
        Assert.Equal(["Normal", "High", "Urgent"], Enum.GetNames<MaintenancePriority>());
    }

    [Fact]
    public void RoomOperations_DoesNotAuthorizeByPositionName()
    {
        var names = typeof(Room).Assembly.GetTypes()
            .Concat(typeof(RoomOperationsDbContext).Assembly.GetTypes())
            .Concat(typeof(RoomOperationsPermissions).Assembly.GetTypes())
            .Select(type => type.Name)
            .ToArray();

        Assert.DoesNotContain(names, name => name.Contains("KatGorevlisi", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, name => name.Contains("SupervisorRole", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, name => name.Contains("OrderTaker", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("room-operations.read", RoomOperationsPermissions.Read);
        Assert.Equal("room-operations.inspect", RoomOperationsPermissions.Inspect);
        Assert.DoesNotContain(names, name => name.Contains("IkMuduru", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, name => name.Contains("IkUzmani", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RoomNumber_IsUniqueWithinProperty_InEfModel()
    {
        var options = new DbContextOptionsBuilder<RoomOperationsDbContext>()
            .UseNpgsql("Host=localhost;Database=huguweb_model_check;Username=huguweb;Password=unused")
            .Options;

        using var context = new RoomOperationsDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Room));
        Assert.NotNull(entity);

        var unique = entity.GetIndexes().Single(index =>
            index.IsUnique
            && index.GetDatabaseName() == RoomOperationsDbContext.RoomNumberIndexName);

        var properties = unique.Properties.Select(property => property.Name).ToArray();
        Assert.Equal(["PropertyId", "Number"], properties);
    }

    [Fact]
    public void RoomReadiness_HasOnlyPreparationStates()
    {
        Assert.Equal(["Dirty", "Clean", "Inspected", "Ready"], Enum.GetNames<RoomReadiness>());
        Assert.Null(typeof(Room).GetProperty("RoomStatus"));
        Assert.Null(typeof(Room).GetProperty("Sellable"));
        Assert.Null(typeof(Room).GetProperty("Occupied"));
    }

    [Fact]
    public void Workforce_HasNoGovernmentIntegrationSurface()
    {
        var names = typeof(Employee).Assembly.GetTypes()
            .Concat(typeof(WorkforceDbContext).Assembly.GetTypes())
            .Concat(typeof(AppIdentityDbContext).Assembly.GetTypes())
            .Select(type => type.Name)
            .ToArray();

        Assert.DoesNotContain("ISgkService", names);
        Assert.DoesNotContain("IKbsService", names);
        Assert.DoesNotContain("IIskurService", names);
        Assert.DoesNotContain("ISgkClient", names);
        Assert.DoesNotContain("IKbsClient", names);
        Assert.DoesNotContain("SgkClient", names);
        Assert.DoesNotContain("KbsClient", names);
        Assert.DoesNotContain("IskurClient", names);
        Assert.DoesNotContain("IGovernmentIntegrationService", names);
    }

    [Fact]
    public void Employee_DoesNotCoupleTo_Identity()
    {
        var propertyNames = typeof(Employee).GetProperties().Select(property => property.Name).ToArray();

        Assert.DoesNotContain("UserId", propertyNames);
        Assert.DoesNotContain("ApplicationUserId", propertyNames);
        Assert.DoesNotContain("ApplicationUser", propertyNames);
    }

    [Fact]
    public void Workforce_DoesNotIntroduce_GenericRepository()
    {
        var names = typeof(Employee).Assembly.GetTypes()
            .Concat(typeof(WorkforceDbContext).Assembly.GetTypes())
            .Select(type => type.Name)
            .ToArray();

        Assert.DoesNotContain("IRepository", names);
        Assert.DoesNotContain("IGenericRepository", names);
        Assert.DoesNotContain("GenericRepository", names);
    }

    [Fact]
    public void PersonnelNumber_IsUniqueWithinOrganization_InEfModel()
    {
        var options = new DbContextOptionsBuilder<WorkforceDbContext>()
            .UseNpgsql("Host=localhost;Database=huguweb_model_check;Username=huguweb;Password=unused")
            .Options;

        using var context = new WorkforceDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Employee));
        Assert.NotNull(entity);

        var unique = entity.GetIndexes().Single(index =>
            index.IsUnique
            && index.GetDatabaseName() == WorkforceDbContext.PersonnelNumberIndexName);

        var properties = unique.Properties.Select(property => property.Name).ToArray();
        Assert.Equal(["OrganizationId", "PersonnelNumber"], properties);
        Assert.Equal("Id", entity.FindPrimaryKey()!.Properties.Single().Name);
    }

    [Fact]
    public void Employee_CoreRemainsMinimal()
    {
        var propertyNames = typeof(Employee).GetProperties().Select(property => property.Name).OrderBy(name => name).ToArray();
        Assert.Equal(["FamilyName", "GivenName", "Id", "OrganizationId", "PersonnelNumber"], propertyNames);
    }

    [Fact]
    public void NationalIdentity_IsUniqueWhenPresent_InEfModel()
    {
        var options = new DbContextOptionsBuilder<WorkforceDbContext>()
            .UseNpgsql("Host=localhost;Database=huguweb_model_check;Username=huguweb;Password=unused")
            .Options;

        using var context = new WorkforceDbContext(options);
        var entity = context.Model.FindEntityType(typeof(EmployeeHrProfile));
        Assert.NotNull(entity);

        var unique = entity.GetIndexes().Single(index =>
            index.IsUnique
            && index.GetDatabaseName() == WorkforceDbContext.NationalIdentityIndexName);

        var properties = unique.Properties.Select(property => property.Name).ToArray();
        Assert.Equal(["OrganizationId", "NationalIdentityScheme", "NormalizedNationalIdentityNumber"], properties);
        Assert.Contains("NormalizedNationalIdentityNumber", unique.GetFilter(), StringComparison.Ordinal);
        Assert.Contains("IS NOT NULL", unique.GetFilter(), StringComparison.Ordinal);
    }

    [Fact]
    public void OperationalEmployeeReference_ContainsNoSensitiveHrFields()
    {
        foreach (var type in new[]
                 {
                     typeof(HuGuWeb.RoomOperations.Application.AssignableEmployee),
                     typeof(HuGuWeb.TechnicalService.Application.AssignableEmployee),
                     typeof(EmployeeDirectoryItem),
                     typeof(ActiveWorkforceMember)
                 })
        {
            var names = type.GetProperties().Select(property => property.Name).ToArray();
            Assert.Contains("EmployeeId", names);
            Assert.DoesNotContain("NationalIdentityNumber", names);
            Assert.DoesNotContain("BirthDate", names);
            Assert.DoesNotContain("ResidenceAddress", names);
            Assert.DoesNotContain("EmergencyContacts", names);
            Assert.DoesNotContain("BloodType", names);
            Assert.DoesNotContain("Iban", names);
            Assert.DoesNotContain("Salary", names);
            Assert.DoesNotContain("Wage", names);
            Assert.DoesNotContain("OccupationCode", names);
            Assert.DoesNotContain("DutyCode", names);
            Assert.DoesNotContain("DocumentTypeCode", names);
            Assert.DoesNotContain("ApplicableLawCode", names);
            Assert.DoesNotContain("InsuranceBranchCode", names);
            Assert.DoesNotContain("SgkWorkplaceRegistrationId", names);
            Assert.DoesNotContain("SgkWorkplaceRegistration", names);
            Assert.DoesNotContain("Nationality", names);
            Assert.DoesNotContain("KepAddress", names);
            Assert.DoesNotContain("MilitaryServiceStatus", names);
            Assert.DoesNotContain("WorkPermitStartDate", names);
            Assert.DoesNotContain("BesRatePercent", names);
            Assert.DoesNotContain("IskurStatus", names);
            Assert.DoesNotContain("EducationDescription", names);
        }

        Assert.Equal(
            ["EmployeeId", "FamilyName", "GivenName", "PersonnelNumber"],
            typeof(HuGuWeb.RoomOperations.Application.AssignableEmployee)
                .GetProperties()
                .Select(property => property.Name)
                .OrderBy(name => name)
                .ToArray());
        Assert.Equal(
            ["EmployeeId", "FamilyName", "GivenName", "PersonnelNumber"],
            typeof(HuGuWeb.TechnicalService.Application.AssignableEmployee)
                .GetProperties()
                .Select(property => property.Name)
                .OrderBy(name => name)
                .ToArray());
    }

    [Fact]
    public void RoomOperationsAndTechnicalService_DoNotReferenceHrProfileImplementation()
    {
        var names = typeof(Room).Assembly.GetTypes()
            .Concat(typeof(RoomOperationsDbContext).Assembly.GetTypes())
            .Concat(typeof(MaintenanceIssue).Assembly.GetTypes())
            .Concat(typeof(TechnicalServiceDbContext).Assembly.GetTypes())
            .Select(type => type.Name)
            .ToArray();

        Assert.DoesNotContain("EmployeeHrProfile", names);
        Assert.DoesNotContain("EmergencyContact", names);
        Assert.DoesNotContain("EmployeePhoto", names);
        Assert.DoesNotContain("HrEmployeeCard", names);
        Assert.DoesNotContain("NationalIdentity", names);
        Assert.DoesNotContain("OfficialEmploymentProfile", names);
        Assert.DoesNotContain("SgkWorkplaceRegistration", names);
        Assert.DoesNotContain("SgkOccupationCode", names);
        Assert.DoesNotContain("SgkDocumentType", names);
        Assert.DoesNotContain("ApplicableLawCode", names);
        Assert.DoesNotContain("InsuranceBranch", names);
    }

    [Fact]
    public void Hr01B_DoesNotIntroduceDeferredHrPlatformTypes()
    {
        var names = typeof(Employee).Assembly.GetTypes()
            .Concat(typeof(WorkforceDbContext).Assembly.GetTypes())
            .Concat(typeof(AppIdentityDbContext).Assembly.GetTypes())
            .Select(type => type.Name)
            .ToArray();

        Assert.DoesNotContain("EmployeePaymentProfile", names);
        Assert.DoesNotContain("PayrollRun", names);
        Assert.DoesNotContain("LeaveBalance", names);
        Assert.DoesNotContain("LeaveRequest", names);
        Assert.DoesNotContain("ShiftAssignment", names);
        Assert.DoesNotContain("ISgkService", names);
        Assert.DoesNotContain("IKbsService", names);
        Assert.DoesNotContain("IIskurService", names);
        Assert.DoesNotContain("SgkClient", names);
        Assert.DoesNotContain("KbsClient", names);
        Assert.DoesNotContain("GovernmentCode", names);
        Assert.DoesNotContain("OfficialCode", names);
        Assert.DoesNotContain("GenericLookup", names);
        Assert.DoesNotContain("Grade", names);
        Assert.DoesNotContain("WorkingGroup", names);
        Assert.DoesNotContain("IOutbox", names);
        Assert.DoesNotContain("OutboxMessage", names);
        Assert.DoesNotContain("IMessageBroker", names);
        Assert.DoesNotContain("IRepository", names);
        Assert.DoesNotContain("IGenericRepository", names);
        Assert.Null(typeof(Employee).GetProperty("UserId"));
        Assert.Null(typeof(Position).GetProperty("Permission"));
        Assert.Null(typeof(Department).GetProperty("Permission"));
    }

    [Fact]
    public void Position_IsPropertyScoped_NotDepartmentOwned()
    {
        Assert.NotNull(typeof(Position).GetProperty(nameof(Position.PropertyId)));
        Assert.Null(typeof(Position).GetProperty("DepartmentId"));

        var options = new DbContextOptionsBuilder<WorkforceDbContext>()
            .UseNpgsql("Host=localhost;Database=huguweb_model_check;Username=huguweb;Password=unused")
            .Options;

        using var context = new WorkforceDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Position));
        Assert.NotNull(entity);
        Assert.NotNull(entity.FindProperty(nameof(Position.PropertyId)));
        Assert.Null(entity.FindProperty("DepartmentId"));

        var departmentForeignKeys = entity.GetForeignKeys()
            .Where(key => key.PrincipalEntityType.ClrType == typeof(Department));
        Assert.Empty(departmentForeignKeys);

        var propertyForeignKeys = entity.GetForeignKeys()
            .Where(key => key.PrincipalEntityType.ClrType == typeof(Property))
            .ToArray();
        Assert.Single(propertyForeignKeys);
        Assert.Equal("PropertyId", propertyForeignKeys[0].Properties.Single().Name);
    }

    [Fact]
    public void DepartmentPositionApplicability_IsAssignabilityOnly_NotOwnershipOrAuthorization()
    {
        Assert.Null(typeof(Position).GetProperty("DepartmentId"));
        Assert.Equal(
            ["DepartmentId", "PositionId"],
            typeof(DepartmentPositionApplicability).GetProperties().Select(property => property.Name).OrderBy(name => name).ToArray());
        Assert.Null(typeof(DepartmentPositionApplicability).GetProperty("Permission"));
        Assert.Null(typeof(DepartmentPositionApplicability).GetProperty("Role"));

        var options = new DbContextOptionsBuilder<WorkforceDbContext>()
            .UseNpgsql("Host=localhost;Database=huguweb_model_check;Username=huguweb;Password=unused")
            .Options;

        using var context = new WorkforceDbContext(options);
        var position = context.Model.FindEntityType(typeof(Position));
        var applicability = context.Model.FindEntityType(typeof(DepartmentPositionApplicability));
        Assert.NotNull(position);
        Assert.NotNull(applicability);
        Assert.Null(position.FindProperty("DepartmentId"));
        Assert.Equal(
            ["DepartmentId", "PositionId"],
            applicability.FindPrimaryKey()!.Properties.Select(property => property.Name).OrderBy(name => name).ToArray());
        Assert.Contains(
            applicability.GetForeignKeys(),
            key => key.PrincipalEntityType.ClrType == typeof(Department));
        Assert.Contains(
            applicability.GetForeignKeys(),
            key => key.PrincipalEntityType.ClrType == typeof(Position));
    }

    [Fact]
    public void PersonnelNumberSequence_IsOrganizationScoped()
    {
        var options = new DbContextOptionsBuilder<WorkforceDbContext>()
            .UseNpgsql("Host=localhost;Database=huguweb_model_check;Username=huguweb;Password=unused")
            .Options;

        using var context = new WorkforceDbContext(options);
        var entity = context.Model.FindEntityType(typeof(PersonnelNumberSequence));
        Assert.NotNull(entity);
        Assert.Equal("OrganizationId", entity.FindPrimaryKey()!.Properties.Single().Name);
        Assert.Equal(PersonnelNumberSequence.StartingValue, 1001);
        Assert.Equal("1001", PersonnelNumber.Format(PersonnelNumberSequence.StartingValue));
    }

    [Fact]
    public void OfficialEmployment_UsesExplicitLookups_NotGenericGovernmentCode()
    {
        Assert.NotNull(typeof(OfficialEmploymentProfile).GetProperty(nameof(OfficialEmploymentProfile.EmploymentId)));
        Assert.NotNull(typeof(OfficialEmploymentProfile).GetProperty(nameof(OfficialEmploymentProfile.SgkWorkplaceRegistrationId)));
        Assert.NotNull(typeof(OfficialEmploymentProfile).GetProperty(nameof(OfficialEmploymentProfile.DocumentTypeCode)));
        Assert.NotNull(typeof(OfficialEmploymentProfile).GetProperty(nameof(OfficialEmploymentProfile.ApplicableLawCode)));
        Assert.NotNull(typeof(OfficialEmploymentProfile).GetProperty(nameof(OfficialEmploymentProfile.InsuranceBranchCode)));
        Assert.NotNull(typeof(OfficialEmploymentProfile).GetProperty(nameof(OfficialEmploymentProfile.OccupationCode)));
        Assert.NotNull(typeof(OfficialEmploymentProfile).GetProperty(nameof(OfficialEmploymentProfile.DutyCode)));
        Assert.Null(typeof(OfficialEmploymentProfile).GetProperty("OccupationName"));
        Assert.Null(typeof(Employee).GetProperty("OccupationCode"));
        Assert.Null(typeof(Employee).GetProperty("BesRatePercent"));
        Assert.Null(typeof(Position).GetProperty("OccupationCode"));
        Assert.Null(typeof(Position).GetProperty("OccupationCodeId"));
        Assert.Null(typeof(Position).GetProperty("DutyCode"));
        Assert.Null(typeof(Property).GetProperty("SgkRegistrationNumber"));
        Assert.Null(typeof(EmployeeHrProfile).GetProperty("MothersMaidenName"));
        Assert.Null(typeof(EmployeeHrProfile).GetProperty("PassportNumber"));
        Assert.Null(typeof(EmployeeHrProfile).GetProperty("VisaStartDate"));

        var names = typeof(Employee).Assembly.GetTypes().Select(type => type.Name).ToArray();
        Assert.Contains("SgkDocumentType", names);
        Assert.Contains("ApplicableLawCode", names);
        Assert.Contains("InsuranceBranch", names);
        Assert.Contains("SgkOccupationCode", names);
        Assert.Contains("SgkWorkplaceRegistration", names);
        Assert.Contains("EmploymentDutyCode", names);
        Assert.Contains("EmploymentBesSettings", names);
        Assert.DoesNotContain("GovernmentCode", names);
        Assert.DoesNotContain("OfficialCode", names);
        Assert.DoesNotContain("GenericLookup", names);
        Assert.DoesNotContain("SgkDutyCode", names);
        Assert.DoesNotContain("AgiSettings", names);
        Assert.DoesNotContain("PayrollRun", names);

        var options = new DbContextOptionsBuilder<WorkforceDbContext>()
            .UseNpgsql("Host=localhost;Database=huguweb_model_check;Username=huguweb;Password=unused")
            .Options;
        using var context = new WorkforceDbContext(options);

        var profile = context.Model.FindEntityType(typeof(OfficialEmploymentProfile));
        Assert.NotNull(profile);
        Assert.Contains(profile.GetIndexes(), index => index.IsUnique && index.Properties.Any(property => property.Name == "EmploymentId"));

        var registrations = context.Model.FindEntityType(typeof(SgkWorkplaceRegistration));
        Assert.NotNull(registrations);
        Assert.DoesNotContain(
            registrations.GetIndexes(),
            index => index.IsUnique && index.Properties.Any(property => property.Name == "IsActive"));
        Assert.Contains(
            registrations.GetForeignKeys(),
            key => key.PrincipalEntityType.ClrType == typeof(Property) && key.DeleteBehavior == DeleteBehavior.Restrict);

        Assert.DoesNotContain(
            context.Model.GetEntityTypes().Select(entity => entity.ClrType.Name),
            name => name is "IOutbox" or "OutboxMessage" or "IMessageBroker" or "NotificationJob");
    }

    [Fact]
    public void OfficialEmployment_Authorization_IsClaimBased_NotEmail()
    {
        var source = string.Join(
            '\n',
            Directory.GetFiles(
                    Path.Combine(FindRepoRoot(), "src", "backend", "modules", "HuGuWeb.Workforce", "Application"),
                    "*Official*.cs")
                .Select(File.ReadAllText));
        Assert.DoesNotContain("@localhost", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hr.manager@", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("user.Email", File.ReadAllText(
            Path.Combine(FindRepoRoot(), "src", "backend", "HuGuWeb.Api", "Endpoints", "HrEmployeeEndpoints.cs")));
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

    [Fact]
    public void Position_HasNoNameUniquenessConstraint()
    {
        var options = new DbContextOptionsBuilder<WorkforceDbContext>()
            .UseNpgsql("Host=localhost;Database=huguweb_model_check;Username=huguweb;Password=unused")
            .Options;

        using var context = new WorkforceDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Position));
        Assert.NotNull(entity);
        Assert.DoesNotContain(entity.GetIndexes(), index => index.IsUnique);
        Assert.DoesNotContain(
            entity.GetKeys().Where(key => key.IsPrimaryKey()),
            key => key.Properties.Any(property => property.Name == nameof(Position.Name)));
    }

    private static void AssertNoDeferredDomains(IEnumerable<string> names)
    {
        string[] forbidden =
        [
            "Reservation",
            "Stay",
            "Minibar",
            "TechnicalService",
            "OutOfOrder",
            "OutOfService",
            "RoomBlock",
            "ISgkService",
            "IKbsService"
        ];

        foreach (var name in names)
        {
            Assert.DoesNotContain(name, forbidden);
        }
    }

    private static void AssertNoRejectedInfrastructure(System.Reflection.Assembly assembly)
    {
        var referencedNames = GetReferencedAssemblyNames(assembly);
        string[] forbidden =
        [
            "MediatR",
            "StackExchange.Redis",
            "MassTransit",
            "RabbitMQ.Client",
            "Hangfire.Core",
            "Hangfire.AspNetCore",
            "Microsoft.EntityFrameworkCore.Sqlite",
            "Microsoft.EntityFrameworkCore.InMemory",
            "MongoDB.Driver"
        ];

        foreach (var name in forbidden)
        {
            Assert.DoesNotContain(name, referencedNames);
        }
    }

    private static string[] GetReferencedAssemblyNames(System.Reflection.Assembly assembly) =>
        assembly
            .GetReferencedAssemblies()
            .Select(name => name.Name ?? string.Empty)
            .ToArray();
}
