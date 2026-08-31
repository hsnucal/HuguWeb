using HuGuWeb.Workforce.Domain;
using HuGuWeb.Workforce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HuGuWeb.Workforce.Infrastructure.Seeding;

/// <summary>
/// DEVELOPMENT-ONLY: rebuilds deterministic persona Employees / Employments / Assignments.
/// </summary>
public static class DevelopmentPersonaEmployeeSeeder
{
    public static async Task EnsureAsync(
        WorkforceDbContext dbContext,
        ILogger logger,
        bool isDevelopment,
        CancellationToken cancellationToken = default)
    {
        if (!isDevelopment)
        {
            throw new InvalidOperationException(
                "Development persona employee seed is blocked outside Development.");
        }

        await EnsureRequiredDepartmentsAndPositionsAsync(dbContext, logger, cancellationToken);

        var keep = DevelopmentPersonaEmployeeFixtures.EmployeeIds;
        var hasForeign = await dbContext.Employees.AnyAsync(
            item => !keep.Contains(item.Id),
            cancellationToken);
        var missingPersona = false;
        foreach (var fixture in DevelopmentPersonaEmployeeFixtures.All)
        {
            if (!await dbContext.Employees.AnyAsync(item => item.Id == fixture.EmployeeId, cancellationToken))
            {
                missingPersona = true;
                break;
            }
        }

        if (hasForeign || missingPersona)
        {
            await DevelopmentOperationalPersonnelReset.ClearAsync(
                dbContext,
                logger,
                isDevelopment: true,
                cancellationToken);
        }

        foreach (var fixture in DevelopmentPersonaEmployeeFixtures.All)
        {
            await EnsureFixtureAsync(dbContext, logger, fixture, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Development persona employees ensured ({Count}).",
            DevelopmentPersonaEmployeeFixtures.All.Count);
    }

    private static async Task EnsureRequiredDepartmentsAndPositionsAsync(
        WorkforceDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        await EnsureDepartmentByCodeAsync(
            dbContext,
            DevelopmentWorkforceSeeder.AnkaraPropertyId,
            DevelopmentWorkforceSeeder.HumanResourcesDepartmentId,
            "İnsan Kaynakları",
            "HR",
            cancellationToken);
        await EnsureDepartmentByCodeAsync(
            dbContext,
            DevelopmentWorkforceSeeder.AnkaraPropertyId,
            DevelopmentWorkforceSeeder.TechnicalDepartmentId,
            "Teknik Servis",
            "ENG",
            cancellationToken);
        await EnsureDepartmentByCodeAsync(
            dbContext,
            DevelopmentWorkforceSeeder.AnkaraPropertyId,
            DevelopmentWorkforceSeeder.HousekeepingDepartmentId,
            "Kat Hizmetleri",
            "HK",
            cancellationToken);

        await EnsureDepartmentByCodeAsync(
            dbContext,
            DevelopmentWorkforceSeeder.AntalyaPropertyId,
            Guid.Parse("a1e1c0de-0001-4000-8000-000000000201"),
            "İnsan Kaynakları",
            "HR",
            cancellationToken);
        await EnsureDepartmentByCodeAsync(
            dbContext,
            DevelopmentWorkforceSeeder.AntalyaPropertyId,
            Guid.Parse("a1e1c0de-0001-4000-8000-000000000204"),
            "Teknik Servis",
            "ENG",
            cancellationToken);
        await EnsureDepartmentByCodeAsync(
            dbContext,
            DevelopmentWorkforceSeeder.AntalyaPropertyId,
            Guid.Parse("a1e1c0de-0001-4000-8000-000000000202"),
            "Kat Hizmetleri",
            "HK",
            cancellationToken);

        foreach (var propertyId in new[]
                 {
                     DevelopmentWorkforceSeeder.AnkaraPropertyId,
                     DevelopmentWorkforceSeeder.AntalyaPropertyId
                 })
        {
            await EnsurePositionAsync(dbContext, propertyId, "Teknisyen", "ENG-TECH", cancellationToken);
            await EnsurePositionAsync(dbContext, propertyId, "İK Uzmanı", "HR-OFF", cancellationToken);
            await EnsurePositionAsync(dbContext, propertyId, "Kat Görevlisi", "HK-ATT", cancellationToken);
            await EnsurePositionAsync(dbContext, propertyId, "Kat Hizmetleri Sorumlusu", "HK-SUP", cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Development departments/positions required by persona employees were ensured.");
    }

    private static async Task EnsureDepartmentByCodeAsync(
        WorkforceDbContext dbContext,
        Guid propertyId,
        Guid preferredId,
        string name,
        string code,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Departments.AnyAsync(
                item => item.PropertyId == propertyId && item.Code == code,
                cancellationToken))
        {
            return;
        }

        var id = preferredId;
        if (await dbContext.Departments.AnyAsync(item => item.Id == preferredId, cancellationToken))
        {
            id = Guid.CreateVersion7();
        }

        if (!Department.TryCreate(id, propertyId, name, code, out var department, out var error)
            || department is null)
        {
            throw new InvalidOperationException($"Development department ensure failed for {code}: {error}");
        }

        dbContext.Departments.Add(department);
    }

    private static async Task EnsurePositionAsync(
        WorkforceDbContext dbContext,
        Guid propertyId,
        string name,
        string code,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Positions.AnyAsync(
                item => item.PropertyId == propertyId && item.Code == code,
                cancellationToken))
        {
            return;
        }

        if (!Position.TryCreate(Guid.CreateVersion7(), propertyId, name, code, out var position, out var error)
            || position is null)
        {
            throw new InvalidOperationException($"Development position ensure failed for {code}: {error}");
        }

        dbContext.Positions.Add(position);
    }

    private static async Task EnsureFixtureAsync(
        WorkforceDbContext dbContext,
        ILogger logger,
        PersonaEmployeeFixture fixture,
        CancellationToken cancellationToken)
    {
        var department = await ResolveDepartmentAsync(dbContext, fixture, cancellationToken);
        var position = await ResolvePositionAsync(dbContext, fixture, cancellationToken);
        if (department is null || position is null)
        {
            logger.LogWarning(
                "Persona employee {PersonnelNumber} was not seeded (department/position missing for {Email}).",
                fixture.PersonnelNumber,
                fixture.PersonaEmail);
            return;
        }

        var employeeExists = await dbContext.Employees.AnyAsync(
            item => item.Id == fixture.EmployeeId,
            cancellationToken);
        if (!employeeExists)
        {
            if (!Employee.TryCreate(
                    fixture.EmployeeId,
                    DevelopmentWorkforceSeeder.OrganizationId,
                    fixture.GivenName,
                    fixture.FamilyName,
                    fixture.PersonnelNumber,
                    out var employee,
                    out var employeeError)
                || employee is null)
            {
                throw new InvalidOperationException(
                    $"Persona employee {fixture.PersonnelNumber} is invalid: {employeeError}");
            }

            dbContext.Employees.Add(employee);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (!await dbContext.Employments.AnyAsync(item => item.Id == fixture.EmploymentId, cancellationToken))
        {
            dbContext.Employments.Add(Employment.Open(
                fixture.EmploymentId,
                fixture.EmployeeId,
                DevelopmentPersonaEmployeeFixtures.EmploymentStartDate,
                today));
        }

        if (!await dbContext.Assignments.AnyAsync(item => item.Id == fixture.AssignmentId, cancellationToken))
        {
            dbContext.Assignments.Add(Assignment.StartPrimary(
                fixture.AssignmentId,
                fixture.EmploymentId,
                department.Id,
                position.Id,
                DevelopmentPersonaEmployeeFixtures.EmploymentStartDate));
        }
    }

    private static async Task<Department?> ResolveDepartmentAsync(
        WorkforceDbContext dbContext,
        PersonaEmployeeFixture fixture,
        CancellationToken cancellationToken)
    {
        var codes = new List<string> { fixture.DepartmentCode };
        if (fixture.AlternateDepartmentCodes is not null)
        {
            codes.AddRange(fixture.AlternateDepartmentCodes);
        }

        foreach (var code in codes)
        {
            var match = await dbContext.Departments.FirstOrDefaultAsync(
                item => item.PropertyId == fixture.PropertyId && item.Code == code && item.IsActive,
                cancellationToken);
            if (match is not null)
            {
                return match;
            }
        }

        return await dbContext.Departments.FirstOrDefaultAsync(
            item => item.Id == fixture.PreferredDepartmentId,
            cancellationToken);
    }

    private static async Task<Position?> ResolvePositionAsync(
        WorkforceDbContext dbContext,
        PersonaEmployeeFixture fixture,
        CancellationToken cancellationToken)
    {
        var codes = new List<string> { fixture.PositionCode };
        if (fixture.AlternatePositionCodes is not null)
        {
            codes.AddRange(fixture.AlternatePositionCodes);
        }

        foreach (var code in codes)
        {
            var match = await dbContext.Positions.FirstOrDefaultAsync(
                item => item.PropertyId == fixture.PropertyId && item.Code == code && item.IsActive,
                cancellationToken);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }
}
