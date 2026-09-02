using HuGuWeb.Workforce.Domain;
using HuGuWeb.Workforce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HuGuWeb.Workforce.Infrastructure.Seeding;

public static class DevelopmentWorkforceSeeder
{
    public static readonly Guid OrganizationId = Guid.Parse("a1e1c0de-0001-4000-8000-000000000001");
    public static readonly Guid AnkaraPropertyId = Guid.Parse("a1e1c0de-0001-4000-8000-000000000002");
    public static readonly Guid AntalyaPropertyId = Guid.Parse("a1e1c0de-0001-4000-8000-000000000003");
    public static readonly Guid PropertyId = AnkaraPropertyId;
    public static readonly Guid DevelopmentEmployeeId = Guid.Parse("a1e1c0de-0003-4000-8000-000000000201");
    public const string DevelopmentPersonnelNumber = "DEV-2001";
    public const string DevelopmentTimeZoneId = "Europe/Istanbul";

    public static readonly Guid HumanResourcesDepartmentId = Guid.Parse("a1e1c0de-0001-4000-8000-000000000101");
    public static readonly Guid HousekeepingDepartmentId = Guid.Parse("a1e1c0de-0001-4000-8000-000000000102");
    public static readonly Guid FrontOfficeDepartmentId = Guid.Parse("a1e1c0de-0001-4000-8000-000000000103");
    public static readonly Guid TechnicalDepartmentId = Guid.Parse("a1e1c0de-0001-4000-8000-000000000104");
    public static readonly Guid FoodBeverageDepartmentId = Guid.Parse("a1e1c0de-0001-4000-8000-000000000105");

    private static readonly Guid HumanResourcesId = HumanResourcesDepartmentId;
    private static readonly Guid HousekeepingId = HousekeepingDepartmentId;
    private static readonly Guid FrontOfficeId = FrontOfficeDepartmentId;
    private static readonly Guid TechnicalId = TechnicalDepartmentId;
    private static readonly Guid FoodBeverageId = FoodBeverageDepartmentId;

    public static async Task TrySeedAsync(
        WorkforceDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken = default,
        bool isDevelopment = true)
    {
        try
        {
            if (!isDevelopment)
            {
                throw new InvalidOperationException(
                    "DevelopmentWorkforceSeeder.TrySeedAsync is blocked outside Development.");
            }

            var workplaceAlreadyPresent = await dbContext.Organizations.AnyAsync(
                entity => entity.Id == OrganizationId,
                cancellationToken);

            if (!workplaceAlreadyPresent)
            {
                dbContext.Organizations.Add(new Organization(OrganizationId, "Demo Hotel Group"));
            }

            if (!await dbContext.Properties.AnyAsync(entity => entity.Id == AnkaraPropertyId, cancellationToken))
            {
                dbContext.Properties.Add(new Property(
                    AnkaraPropertyId,
                    OrganizationId,
                    "Ankara Hotel",
                    DevelopmentTimeZoneId));
            }

            if (!await dbContext.Properties.AnyAsync(entity => entity.Id == AntalyaPropertyId, cancellationToken))
            {
                dbContext.Properties.Add(new Property(
                    AntalyaPropertyId,
                    OrganizationId,
                    "Antalya Hotel",
                    DevelopmentTimeZoneId));
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await RelabelDevelopmentWorkplaceAsync(dbContext, cancellationToken);

            if (!await dbContext.Departments.AnyAsync(
                    entity => entity.PropertyId == PropertyId,
                    cancellationToken))
            {
                AddDepartment(dbContext, HumanResourcesId, "İnsan Kaynakları", "HR");
                AddDepartment(dbContext, HousekeepingId, "Kat Hizmetleri", "HK");
                AddDepartment(dbContext, FrontOfficeId, "Ön Büro", "FO");
                AddDepartment(dbContext, TechnicalId, "Teknik Servis", "ENG");
                AddDepartment(dbContext, FoodBeverageId, "Yiyecek ve İçecek", "FNB");
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            if (!await dbContext.Positions.AnyAsync(
                    position => position.PropertyId == PropertyId,
                    cancellationToken))
            {
                AddPosition(dbContext, "Kat Görevlisi", "HK-ATT");
                AddPosition(dbContext, "Kat Hizmetleri Sorumlusu", "HK-SUP");
                AddPosition(dbContext, "Minibar Görevlisi", "HK-MIN");
                AddPosition(dbContext, "Resepsiyon Görevlisi", "FO-REC");
                AddPosition(dbContext, "Ön Büro Müdürü", "FO-MGR");
                AddPosition(dbContext, "Garson", "FNB-WAIT");
                AddPosition(dbContext, "Aşçı", "FNB-CHEF");
                AddPosition(dbContext, "Teknisyen", "ENG-TECH");
                AddPosition(dbContext, "İK Uzmanı", "HR-OFF");
                AddPosition(dbContext, "Uzman", "SPEC");
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                await EnsurePositionAsync(dbContext, "Ön Büro Müdürü", "FO-MGR", cancellationToken);
                await EnsurePositionAsync(dbContext, "Garson", "FNB-WAIT", cancellationToken);
                await EnsurePositionAsync(dbContext, "Aşçı", "FNB-CHEF", cancellationToken);
                await EnsurePositionAsync(dbContext, "Teknisyen", "ENG-TECH", cancellationToken);
                await EnsurePositionAsync(dbContext, "İK Uzmanı", "HR-OFF", cancellationToken);
            }

            await TrySeedAntalyaStructureAsync(dbContext, logger, cancellationToken);
            await DevelopmentPersonaEmployeeSeeder.EnsureAsync(
                dbContext,
                logger,
                isDevelopment: true,
                cancellationToken);

            await TrySeedPersonnelNumberSequenceAsync(dbContext, cancellationToken);
            await TrySeedPositionApplicabilityAsync(dbContext, logger, cancellationToken);
            await TrySeedOfficialEmploymentLookupsAsync(dbContext, logger, cancellationToken);
            await TrySeedSgkWorkplaceRegistrationsAsync(dbContext, logger, cancellationToken);

            logger.LogInformation(
                "Development workplace is available. Organization {OrganizationId}, Property {PropertyId}.",
                OrganizationId,
                PropertyId);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Development workforce data was not seeded because the database is unavailable.");
        }
    }

    private static async Task RelabelDevelopmentWorkplaceAsync(
        WorkforceDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var organization = await dbContext.Organizations.FirstOrDefaultAsync(
            entity => entity.Id == OrganizationId,
            cancellationToken);
        organization?.Rename("Demo Hotel Group");

        var ankara = await dbContext.Properties.FirstOrDefaultAsync(
            entity => entity.Id == AnkaraPropertyId,
            cancellationToken);
        ankara?.Rename("Ankara Hotel");
        ankara?.SetTimeZoneId(DevelopmentTimeZoneId);

        var antalya = await dbContext.Properties.FirstOrDefaultAsync(
            entity => entity.Id == AntalyaPropertyId,
            cancellationToken);
        antalya?.Rename("Antalya Hotel");
        antalya?.SetTimeZoneId(DevelopmentTimeZoneId);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void AddDepartment(WorkforceDbContext dbContext, Guid id, string name, string code) =>
        AddDepartment(dbContext, id, PropertyId, name, code);

    private static void AddDepartment(
        WorkforceDbContext dbContext,
        Guid id,
        Guid propertyId,
        string name,
        string code)
    {
        if (!Department.TryCreate(id, propertyId, name, code, out var department, out var error)
            || department is null)
        {
            throw new InvalidOperationException($"Development department seed is invalid: {error}");
        }

        dbContext.Departments.Add(department);
    }

    private static async Task TrySeedAntalyaStructureAsync(
        WorkforceDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var hrId = Guid.Parse("a1e1c0de-0001-4000-8000-000000000201");
        var hkId = Guid.Parse("a1e1c0de-0001-4000-8000-000000000202");
        if (!await dbContext.Departments.AnyAsync(item => item.PropertyId == AntalyaPropertyId, cancellationToken))
        {
            AddDepartment(dbContext, hrId, AntalyaPropertyId, "İnsan Kaynakları", "HR");
            AddDepartment(dbContext, hkId, AntalyaPropertyId, "Kat Hizmetleri", "HK");
            AddDepartment(
                dbContext,
                Guid.Parse("a1e1c0de-0001-4000-8000-000000000203"),
                AntalyaPropertyId,
                "Ön Büro",
                "FO");
            AddDepartment(
                dbContext,
                Guid.Parse("a1e1c0de-0001-4000-8000-000000000204"),
                AntalyaPropertyId,
                "Teknik Servis",
                "ENG");
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.Positions.AnyAsync(item => item.PropertyId == AntalyaPropertyId, cancellationToken))
        {
            AddPosition(dbContext, AntalyaPropertyId, "İK Uzmanı", "HR-OFF");
            AddPosition(dbContext, AntalyaPropertyId, "Kat Görevlisi", "HK-ATT");
            AddPosition(dbContext, AntalyaPropertyId, "Resepsiyon Görevlisi", "FO-REC");
            AddPosition(dbContext, AntalyaPropertyId, "Teknisyen", "ENG-TECH");
            AddPosition(dbContext, AntalyaPropertyId, "Kat Hizmetleri Sorumlusu", "HK-SUP");
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation("Antalya Hotel workplace structure is available.");
    }

    private static async Task TrySeedDevelopmentEmployeeAsync(
        WorkforceDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Employees.AnyAsync(item => item.Id == DevelopmentEmployeeId, cancellationToken))
        {
            return;
        }

        var position = await dbContext.Positions.FirstOrDefaultAsync(
            item => item.PropertyId == PropertyId && item.Code == "SPEC",
            cancellationToken);
        if (position is null)
        {
            logger.LogWarning("Development employee was not seeded because no property position with code SPEC exists.");
            return;
        }

        if (!Employee.TryCreate(
                DevelopmentEmployeeId,
                OrganizationId,
                "Can",
                "Yılmaz",
                DevelopmentPersonnelNumber,
                out var employee,
                out var employeeError)
            || employee is null)
        {
            throw new InvalidOperationException($"Development employee seed is invalid: {employeeError}");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = new DateOnly(2026, 1, 1);
        var employment = Employment.Open(Guid.Parse("a1e1c0de-0003-4000-8000-000000000202"), employee.Id, startDate, today);
        var assignment = Assignment.StartPrimary(
            Guid.Parse("a1e1c0de-0003-4000-8000-000000000203"),
            employment.Id,
            TechnicalId,
            position.Id,
            startDate);

        dbContext.Employees.Add(employee);
        dbContext.Employments.Add(employment);
        dbContext.Assignments.Add(assignment);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Development workforce employee {PersonnelNumber} is available for assignment tests.",
            DevelopmentPersonnelNumber);
    }

    private static async Task TrySeedPersonnelMasterDemoAsync(
        WorkforceDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var housekeepingAttendant = await dbContext.Positions.FirstOrDefaultAsync(
            item => item.PropertyId == PropertyId && item.Code == "HK-ATT",
            cancellationToken);
        var receptionist = await dbContext.Positions.FirstOrDefaultAsync(
            item => item.PropertyId == PropertyId && item.Code == "FO-REC",
            cancellationToken);
        if (housekeepingAttendant is null || receptionist is null)
        {
            logger.LogWarning("Personnel master demo employees were not seeded because expected positions are missing.");
            return;
        }

        await EnsureProfileAsync(
            dbContext,
            DevelopmentEmployeeId,
            new EmployeeHrProfileValues(
                NationalIdentityScheme: null,
                NationalIdentityNumber: null,
                Nationality: "TR",
                Gender: Gender.Male,
                BirthDate: new DateOnly(1994, 3, 18),
                BirthPlace: "İzmir",
                MaritalStatus: MaritalStatus.Single,
                BloodType: BloodType.APositive,
                EducationLevel: EducationLevel.Bachelor,
                MobilePhone: "+905551112233",
                HomePhone: null,
                Email: "can.yilmaz@localhost",
                ResidenceAddress: null,
                ResidenceCity: null,
                ResidenceDistrict: null,
                NotificationAddress: null,
                HrNotes: null,
                DrivingLicenceCategory: null,
                MilitaryServiceStatus: null,
                MilitaryExemptionReason: null,
                MilitaryDefermentReason: null,
                KepAddress: null,
                EducationDescription: null,
                SchoolName: null,
                GraduationDate: null,
                ForeignLanguage: null),
            [],
            today,
            cancellationToken);

        var transferredId = Guid.Parse("a1e1c0de-0003-4000-8000-000000000210");
        if (!await dbContext.Employees.AnyAsync(item => item.Id == transferredId, cancellationToken))
        {
            var seeded = AddEmployee(
                dbContext,
                transferredId,
                "Ayşe",
                "Yılmaz",
                "P-1001",
                new DateOnly(2025, 3, 1),
                today,
                HousekeepingId,
                housekeepingAttendant.Id,
                endedOn: null);
            if (!seeded.Assignment.TryCloseOn(new DateOnly(2026, 1, 31), out var closeError))
            {
                throw new InvalidOperationException($"Transfer seed is invalid: {closeError}");
            }

            dbContext.Assignments.Add(Assignment.StartPrimary(
                Guid.Parse("a1e1c0de-0003-4000-8000-000000000213"),
                seeded.Employment.Id,
                FrontOfficeId,
                receptionist.Id,
                new DateOnly(2026, 2, 1)));
        }

        await EnsureProfileAsync(
            dbContext,
            transferredId,
            new EmployeeHrProfileValues(
                NationalIdentityScheme.Tckn,
                "10000000146",
                "TR",
                Gender.Female,
                new DateOnly(1992, 7, 21),
                "Ankara",
                MaritalStatus.Married,
                BloodType.OPositive,
                EducationLevel.Associate,
                "+905554445566",
                "03121112233",
                "ayse.yilmaz@localhost",
                "Çankaya Mah. Demo Sok. 12",
                "Ankara",
                "Çankaya",
                "Otel lojmanı A-2",
                "Referans transfer geçmişi.",
                null, null, null, null, null, null, null, null, null),
            [
                new EmergencyContactDraft(Guid.Empty, "Mehmet Yılmaz", "Eş", "+905557778899", true),
                new EmergencyContactDraft(Guid.Empty, "Elif Kaya", "Kardeş", "+905559990011", false)
            ],
            today,
            cancellationToken);

        var endedId = Guid.Parse("a1e1c0de-0003-4000-8000-000000000220");
        if (!await dbContext.Employees.AnyAsync(item => item.Id == endedId, cancellationToken))
        {
            AddEmployee(
                dbContext,
                endedId,
                "Mehmet",
                "Kaya",
                "P-1002",
                new DateOnly(2024, 6, 1),
                today,
                HousekeepingId,
                housekeepingAttendant.Id,
                endedOn: new DateOnly(2026, 4, 30));
        }

        await EnsureProfileAsync(
            dbContext,
            endedId,
            new EmployeeHrProfileValues(
                NationalIdentityScheme.Tckn,
                "12345678950",
                "TR",
                Gender.Male,
                new DateOnly(1988, 11, 2),
                "Bursa",
                MaritalStatus.Single,
                BloodType.BPositive,
                EducationLevel.HighSchool,
                "+905553334455",
                null,
                "mehmet.kaya@localhost",
                "Osmangazi Mah. 8",
                "Bursa",
                "Osmangazi",
                null,
                "İşten ayrılmış; profil korunur.",
                null, null, null, null, null, null, null, null, null),
            [new EmergencyContactDraft(Guid.Empty, "Fatma Kaya", "Anne", "+905551234567", true)],
            today,
            cancellationToken);

        var foreignId = Guid.Parse("a1e1c0de-0003-4000-8000-000000000230");
        if (!await dbContext.Employees.AnyAsync(item => item.Id == foreignId, cancellationToken))
        {
            AddEmployee(
                dbContext,
                foreignId,
                "Elena",
                "Popov",
                "P-1003",
                new DateOnly(2026, 5, 1),
                today,
                FoodBeverageId,
                receptionist.Id,
                endedOn: null);
        }

        await EnsureProfileAsync(
            dbContext,
            foreignId,
            new EmployeeHrProfileValues(
                NationalIdentityScheme.Passport,
                "P1234567",
                "RU",
                Gender.Female,
                new DateOnly(1996, 1, 9),
                "Moscow",
                MaritalStatus.Single,
                null,
                EducationLevel.Bachelor,
                "+905556667788",
                null,
                "elena.popov@localhost",
                null,
                null,
                null,
                null,
                "Yabancı personel; TCKN yok.",
                null, null, null, null, null, null, null, null, null),
            [],
            today,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static (Employee Employee, Employment Employment, Assignment Assignment) AddEmployee(
        WorkforceDbContext dbContext,
        Guid employeeId,
        string givenName,
        string familyName,
        string personnelNumber,
        DateOnly startDate,
        DateOnly today,
        Guid departmentId,
        Guid positionId,
        DateOnly? endedOn)
    {
        if (!Employee.TryCreate(employeeId, OrganizationId, givenName, familyName, personnelNumber, out var employee, out var error)
            || employee is null)
        {
            throw new InvalidOperationException($"Development employee seed is invalid: {error}");
        }

        var employment = Employment.Open(Guid.CreateVersion7(), employee.Id, startDate, today);
        var assignment = Assignment.StartPrimary(Guid.CreateVersion7(), employment.Id, departmentId, positionId, startDate);
        if (endedOn is not null)
        {
            if (!employment.TryEnd(endedOn.Value, EmploymentTerminationReason.Resignation, out var endError))
            {
                throw new InvalidOperationException($"Ended employment seed is invalid: {endError}");
            }

            if (!assignment.TryCloseOn(endedOn.Value, out var closeError))
            {
                throw new InvalidOperationException($"Ended assignment seed is invalid: {closeError}");
            }
        }

        dbContext.Employees.Add(employee);
        dbContext.Employments.Add(employment);
        dbContext.Assignments.Add(assignment);
        return (employee, employment, assignment);
    }

    private static async Task EnsureProfileAsync(
        WorkforceDbContext dbContext,
        Guid employeeId,
        EmployeeHrProfileValues values,
        IReadOnlyList<EmergencyContactDraft> contacts,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        if (await dbContext.EmployeeHrProfiles.AnyAsync(item => item.EmployeeId == employeeId, cancellationToken))
        {
            return;
        }

        var employee = await dbContext.Employees.FirstOrDefaultAsync(item => item.Id == employeeId, cancellationToken)
            ?? dbContext.Employees.Local.FirstOrDefault(item => item.Id == employeeId);
        if (employee is null)
        {
            return;
        }

        var profile = EmployeeHrProfile.Create(Guid.CreateVersion7(), employee.Id, employee.OrganizationId);
        if (!profile.TryApply(values, today, out var profileField, out var profileError))
        {
            throw new InvalidOperationException($"HR profile seed is invalid: {profileField} {profileError}");
        }

        dbContext.EmployeeHrProfiles.Add(profile);
        if (!EmergencyContact.TryCreateCollection(employee.Id, contacts, out var created, out var contactField, out var contactError))
        {
            throw new InvalidOperationException($"Emergency contact seed is invalid: {contactField} {contactError}");
        }

        foreach (var contact in created)
        {
            dbContext.EmergencyContacts.Add(contact);
        }
    }

    private static async Task EnsurePositionAsync(
        WorkforceDbContext dbContext,
        string name,
        string code,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Positions.AnyAsync(
                item => item.PropertyId == PropertyId && item.Code == code,
                cancellationToken))
        {
            return;
        }

        AddPosition(dbContext, name, code);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task TrySeedPersonnelNumberSequenceAsync(
        WorkforceDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (await dbContext.PersonnelNumberSequences.AnyAsync(
                item => item.OrganizationId == OrganizationId,
                cancellationToken))
        {
            return;
        }

        dbContext.PersonnelNumberSequences.Add(
            new PersonnelNumberSequence(OrganizationId, PersonnelNumberSequence.StartingValue));
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task TrySeedPositionApplicabilityAsync(
        WorkforceDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var positions = await dbContext.Positions
            .Where(item => item.PropertyId == PropertyId)
            .ToListAsync(cancellationToken);
        if (positions.Count == 0)
        {
            return;
        }

        var positionIds = positions.Select(item => item.Id).ToArray();
        var departments = await dbContext.Departments
            .Where(item => item.PropertyId == PropertyId)
            .ToListAsync(cancellationToken);
        var departmentByCode = departments
            .Where(item => item.Code is not null)
            .ToDictionary(item => item.Code!, item => item.Id, StringComparer.Ordinal);
        var existing = await dbContext.DepartmentPositionApplicabilities
            .Where(item => positionIds.Contains(item.PositionId))
            .Select(item => new { item.DepartmentId, item.PositionId })
            .ToListAsync(cancellationToken);
        var known = existing
            .Select(item => (item.DepartmentId, item.PositionId))
            .ToHashSet();

        void Map(string positionCode, params string[] departmentCodes)
        {
            var position = positions.FirstOrDefault(item => item.Code == positionCode);
            if (position is null)
            {
                return;
            }

            foreach (var departmentCode in departmentCodes)
            {
                if (!departmentByCode.TryGetValue(departmentCode, out var departmentId))
                {
                    continue;
                }

                var key = (departmentId, position.Id);
                if (!known.Add(key))
                {
                    continue;
                }

                dbContext.DepartmentPositionApplicabilities.Add(
                    new DepartmentPositionApplicability(departmentId, position.Id));
            }
        }

        Map("HK-ATT", "HK");
        Map("HK-SUP", "HK");
        Map("HK-MIN", "HK");
        Map("FO-REC", "FO");
        Map("FO-MGR", "FO");
        Map("FNB-WAIT", "FNB");
        Map("FNB-CHEF", "FNB");
        Map("ENG-TECH", "ENG");
        Map("HR-OFF", "HR");
        Map("SPEC", "HR", "ENG");

        var assignments = await dbContext.Assignments
            .Where(item => positionIds.Contains(item.PositionId))
            .Select(item => new { item.DepartmentId, item.PositionId })
            .Distinct()
            .ToListAsync(cancellationToken);
        foreach (var assignment in assignments)
        {
            var key = (assignment.DepartmentId, assignment.PositionId);
            if (!known.Add(key))
            {
                continue;
            }

            dbContext.DepartmentPositionApplicabilities.Add(
                new DepartmentPositionApplicability(assignment.DepartmentId, assignment.PositionId));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Development department/position applicability is available for {Count} mappings.",
            known.Count);
    }

    private static void AddPosition(WorkforceDbContext dbContext, string name, string code) =>
        AddPosition(dbContext, PropertyId, name, code);

    private static void AddPosition(WorkforceDbContext dbContext, Guid propertyId, string name, string code)
    {
        if (!Position.TryCreate(Guid.CreateVersion7(), propertyId, name, code, out var position, out var error)
            || position is null)
        {
            throw new InvalidOperationException($"Development position seed is invalid: {error}");
        }

        dbContext.Positions.Add(position);
    }

    private static async Task TrySeedOfficialEmploymentLookupsAsync(
        WorkforceDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.SgkDocumentTypes.AnyAsync(cancellationToken))
        {
            foreach (var (code, description) in OfficialLookupCatalog.DocumentTypes)
            {
                dbContext.SgkDocumentTypes.Add(new SgkDocumentType(code, description));
            }
        }

        if (!await dbContext.ApplicableLawCodes.AnyAsync(cancellationToken))
        {
            foreach (var (code, description) in OfficialLookupCatalog.ApplicableLaws)
            {
                dbContext.ApplicableLawCodes.Add(new ApplicableLawCode(code, description));
            }
        }

        if (!await dbContext.InsuranceBranches.AnyAsync(cancellationToken))
        {
            foreach (var (code, description) in OfficialLookupCatalog.InsuranceBranches)
            {
                dbContext.InsuranceBranches.Add(new InsuranceBranch(code, description));
            }
        }

        if (!await dbContext.EmploymentDutyCodes.AnyAsync(cancellationToken))
        {
            foreach (var (code, description) in OfficialLookupCatalog.DutyCodes)
            {
                dbContext.EmploymentDutyCodes.Add(new EmploymentDutyCode(code, description));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await SgkOccupationCatalogueImporter.ImportAsync(dbContext, logger, cancellationToken);
    }

    private static readonly Guid HotelWorkplaceId = Guid.Parse("a1e1c0de-0004-4000-8000-000000000001");
    private static readonly Guid RestaurantWorkplaceId = Guid.Parse("a1e1c0de-0004-4000-8000-000000000002");

    private static async Task TrySeedSgkWorkplaceRegistrationsAsync(
        WorkforceDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (await dbContext.SgkWorkplaceRegistrations.AnyAsync(
                item => item.PropertyId == PropertyId,
                cancellationToken))
        {
            return;
        }

        var createdAt = DateTimeOffset.UtcNow;
        if (!SgkWorkplaceRegistration.TryCreate(
                HotelWorkplaceId,
                PropertyId,
                "123456789012345678901",
                "Otel",
                createdAt,
                out var hotel,
                out _,
                out _)
            || hotel is null
            || !SgkWorkplaceRegistration.TryCreate(
                RestaurantWorkplaceId,
                PropertyId,
                "123456789012345678902",
                "Restoran",
                createdAt,
                out var restaurant,
                out _,
                out _)
            || restaurant is null)
        {
            logger.LogWarning("Development SGK workplace registrations were not seeded because the numbers are invalid.");
            return;
        }

        dbContext.SgkWorkplaceRegistrations.Add(hotel);
        dbContext.SgkWorkplaceRegistrations.Add(restaurant);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Development SGK workplace registrations are available for the seeded property.");
    }
}
