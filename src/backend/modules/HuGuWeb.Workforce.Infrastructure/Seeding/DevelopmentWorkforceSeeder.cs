using HuGuWeb.Workforce.Domain;
using HuGuWeb.Workforce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HuGuWeb.Workforce.Infrastructure.Seeding;

public static class DevelopmentWorkforceSeeder
{
    public static readonly Guid OrganizationId = Guid.Parse("a1e1c0de-0001-4000-8000-000000000001");
    public static readonly Guid PropertyId = Guid.Parse("a1e1c0de-0001-4000-8000-000000000002");
    public static readonly Guid DevelopmentEmployeeId = Guid.Parse("a1e1c0de-0003-4000-8000-000000000201");
    public const string DevelopmentPersonnelNumber = "DEV-2001";

    private static readonly Guid HumanResourcesId = Guid.Parse("a1e1c0de-0001-4000-8000-000000000101");
    private static readonly Guid HousekeepingId = Guid.Parse("a1e1c0de-0001-4000-8000-000000000102");
    private static readonly Guid FrontOfficeId = Guid.Parse("a1e1c0de-0001-4000-8000-000000000103");
    private static readonly Guid TechnicalId = Guid.Parse("a1e1c0de-0001-4000-8000-000000000104");
    private static readonly Guid FoodBeverageId = Guid.Parse("a1e1c0de-0001-4000-8000-000000000105");

    public static async Task TrySeedAsync(
        WorkforceDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await dbContext.Organizations.AnyAsync(entity => entity.Id == OrganizationId, cancellationToken))
            {
                dbContext.Organizations.Add(new Organization(OrganizationId, "HuGuWeb Development Hotel"));
            }

            if (!await dbContext.Properties.AnyAsync(entity => entity.Id == PropertyId, cancellationToken))
            {
                dbContext.Properties.Add(new Property(PropertyId, OrganizationId, "HuGuWeb Development Property"));
            }

            await dbContext.SaveChangesAsync(cancellationToken);

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
                AddPosition(dbContext, "Uzman", "SPEC");
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await TrySeedDevelopmentEmployeeAsync(dbContext, logger, cancellationToken);

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

    private static void AddDepartment(WorkforceDbContext dbContext, Guid id, string name, string code)
    {
        if (!Department.TryCreate(id, PropertyId, name, code, out var department, out var error)
            || department is null)
        {
            throw new InvalidOperationException($"Development department seed is invalid: {error}");
        }

        dbContext.Departments.Add(department);
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

    private static void AddPosition(WorkforceDbContext dbContext, string name, string code)
    {
        if (!Position.TryCreate(Guid.CreateVersion7(), PropertyId, name, code, out var position, out var error)
            || position is null)
        {
            throw new InvalidOperationException($"Development position seed is invalid: {error}");
        }

        dbContext.Positions.Add(position);
    }
}
