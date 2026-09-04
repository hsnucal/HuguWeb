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

            await EnsureCatalogWorkplaceAsync(dbContext, AnkaraPropertyId, cancellationToken);
            await EnsureCatalogWorkplaceAsync(dbContext, AntalyaPropertyId, cancellationToken);
            await SyncPositionHierarchyAsync(dbContext, cancellationToken);
            logger.LogInformation("Antalya Hotel workplace structure is available.");
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

    private static async Task EnsureCatalogWorkplaceAsync(
        WorkforceDbContext dbContext,
        Guid propertyId,
        CancellationToken cancellationToken)
    {
        foreach (var (id, name, code) in DevelopmentWorkplaceCatalog.DepartmentsFor(propertyId))
        {
            if (await dbContext.Departments.AnyAsync(
                    item => item.PropertyId == propertyId && item.Code == code,
                    cancellationToken))
            {
                continue;
            }

            var departmentId = id;
            if (await dbContext.Departments.AnyAsync(item => item.Id == id, cancellationToken))
            {
                departmentId = Guid.CreateVersion7();
            }

            AddDepartment(dbContext, departmentId, propertyId, name, code);
        }

        foreach (var (name, code) in DevelopmentWorkplaceCatalog.Positions)
        {
            if (await dbContext.Positions.AnyAsync(
                    item => item.PropertyId == propertyId && item.Code == code,
                    cancellationToken))
            {
                continue;
            }

            AddPosition(dbContext, propertyId, name, code);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SyncPositionHierarchyAsync(
        WorkforceDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var propertyIds = await dbContext.Properties
            .Where(item => item.OrganizationId == OrganizationId)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        var positions = await dbContext.Positions
            .Where(item => propertyIds.Contains(item.PropertyId))
            .ToListAsync(cancellationToken);
        foreach (var position in positions)
        {
            if (position.Code is null)
            {
                continue;
            }

            var (level, canManage) = DevelopmentWorkplaceCatalog.HierarchyForCode(position.Code);
            _ = position.TrySetOrganizationalLevel(level, out _);
            position.SetCanManageEmployees(canManage);
        }

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
        var propertyIds = await dbContext.Properties
            .Where(item => item.OrganizationId == OrganizationId)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        if (propertyIds.Count == 0)
        {
            return;
        }

        var positions = await dbContext.Positions
            .Where(item => propertyIds.Contains(item.PropertyId))
            .ToListAsync(cancellationToken);
        var departments = await dbContext.Departments
            .Where(item => propertyIds.Contains(item.PropertyId))
            .ToListAsync(cancellationToken);
        if (positions.Count == 0)
        {
            return;
        }

        var positionIds = positions.Select(item => item.Id).ToArray();
        var existing = await dbContext.DepartmentPositionApplicabilities
            .Where(item => positionIds.Contains(item.PositionId))
            .Select(item => new { item.DepartmentId, item.PositionId })
            .ToListAsync(cancellationToken);
        var known = existing
            .Select(item => (item.DepartmentId, item.PositionId))
            .ToHashSet();

        foreach (var pair in DevelopmentWorkplaceCatalog.ResolveApplicability(departments, positions))
        {
            if (!known.Add(pair))
            {
                continue;
            }

            dbContext.DepartmentPositionApplicabilities.Add(
                new DepartmentPositionApplicability(pair.DepartmentId, pair.PositionId));
        }

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
            "Development department/position applicability is available for {Count} mappings across {PropertyCount} properties.",
            known.Count,
            propertyIds.Count);
    }

    private static void AddPosition(WorkforceDbContext dbContext, Guid propertyId, string name, string code)
    {
        var (level, canManage) = DevelopmentWorkplaceCatalog.HierarchyForCode(code);
        if (!Position.TryCreate(
                Guid.CreateVersion7(),
                propertyId,
                name,
                code,
                level,
                canManage,
                out var position,
                out var error)
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
