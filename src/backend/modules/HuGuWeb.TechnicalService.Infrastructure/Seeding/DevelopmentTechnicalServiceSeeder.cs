using HuGuWeb.TechnicalService.Domain;
using HuGuWeb.TechnicalService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HuGuWeb.TechnicalService.Infrastructure.Seeding;

public static class DevelopmentTechnicalServiceSeeder
{
    public static readonly Guid KlimaId = Guid.Parse("a1e1c0de-0004-4000-8000-000000000101");
    public static readonly Guid TesisatId = Guid.Parse("a1e1c0de-0004-4000-8000-000000000102");
    public static readonly Guid ElektrikId = Guid.Parse("a1e1c0de-0004-4000-8000-000000000103");
    public static readonly Guid BoyaId = Guid.Parse("a1e1c0de-0004-4000-8000-000000000104");
    public static readonly Guid MobilyaId = Guid.Parse("a1e1c0de-0004-4000-8000-000000000105");
    public static readonly Guid OdaEkipmaniId = Guid.Parse("a1e1c0de-0004-4000-8000-000000000106");
    public static readonly Guid DigerId = Guid.Parse("a1e1c0de-0004-4000-8000-000000000107");

    private static readonly (Guid Id, string Name)[] Categories =
    [
        (KlimaId, "Klima"),
        (TesisatId, "Tesisat"),
        (ElektrikId, "Elektrik"),
        (BoyaId, "Boya"),
        (MobilyaId, "Mobilya"),
        (OdaEkipmaniId, "Oda ekipmanı"),
        (DigerId, "Diğer")
    ];

    public static async Task TrySeedAsync(
        TechnicalServiceDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ankaraId = Guid.Parse("a1e1c0de-0001-4000-8000-000000000002");
            var antalyaId = Guid.Parse("a1e1c0de-0001-4000-8000-000000000003");
            var seeded = 0;

            foreach (var (id, name) in Categories)
            {
                if (await dbContext.Categories.AnyAsync(item => item.Id == id, cancellationToken))
                {
                    continue;
                }

                if (!MaintenanceIssueCategory.TryCreate(id, ankaraId, name, out var category, out var error)
                    || category is null)
                {
                    throw new InvalidOperationException($"Development category seed is invalid: {error}");
                }

                dbContext.Categories.Add(category);
                seeded++;
            }

            (Guid Id, string Name)[] antalyaCategories =
            [
                (Guid.Parse("a1e1c0de-0004-4000-8000-000000000201"), "Klima"),
                (Guid.Parse("a1e1c0de-0004-4000-8000-000000000202"), "Elektrik")
            ];
            foreach (var (id, name) in antalyaCategories)
            {
                if (await dbContext.Categories.AnyAsync(item => item.Id == id, cancellationToken))
                {
                    continue;
                }

                if (!MaintenanceIssueCategory.TryCreate(id, antalyaId, name, out var category, out var error)
                    || category is null)
                {
                    throw new InvalidOperationException($"Antalya category seed is invalid: {error}");
                }

                dbContext.Categories.Add(category);
                seeded++;
            }

            if (seeded > 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            logger.LogInformation(
                "Development technical service categories are available on Ankara and Antalya properties.");
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Development technical service data was not seeded because the database is unavailable.");
        }
    }
}
