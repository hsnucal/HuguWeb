using HuGuWeb.Workforce.Domain;
using HuGuWeb.Workforce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HuGuWeb.Workforce.Infrastructure.Seeding;

public static class SgkOccupationCatalogueImporter
{
    public const string ResourceName = "HuGuWeb.Workforce.Reference.sgk-occupation-codes.json";

    public static string LoadEmbeddedJson()
    {
        var assembly = typeof(SgkOccupationCatalogueImporter).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded occupation catalogue '{ResourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static SgkOccupationCatalogueDocument LoadEmbeddedDocument() =>
        SgkOccupationCatalogueParser.Parse(LoadEmbeddedJson());

    public static async Task ImportAsync(
        WorkforceDbContext dbContext,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var document = LoadEmbeddedDocument();
        var existing = await dbContext.SgkOccupationCodes.ToDictionaryAsync(
            item => item.Code,
            cancellationToken);
        var incoming = document.Occupations.Select(item => item.Code).ToHashSet(StringComparer.Ordinal);
        var inserted = 0;
        var updated = 0;

        foreach (var row in document.Occupations)
        {
            if (existing.TryGetValue(row.Code, out var current))
            {
                if (current.SyncFromCatalogue(row.Name, row.IsActive, document.Source, document.CatalogueVersion))
                {
                    updated++;
                }

                continue;
            }

            dbContext.SgkOccupationCodes.Add(
                new SgkOccupationCode(
                    row.Code,
                    row.Name,
                    row.IsActive,
                    document.Source,
                    document.CatalogueVersion));
            inserted++;
        }

        var deactivated = 0;
        foreach (var current in existing.Values)
        {
            if (incoming.Contains(current.Code) || !current.IsActive)
            {
                continue;
            }

            current.Deactivate();
            deactivated++;
        }

        if (inserted > 0 || updated > 0 || deactivated > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        logger.LogInformation(
            "SGK occupation catalogue {Version} imported. Rows {Count}, inserted {Inserted}, updated {Updated}, deactivated {Deactivated}.",
            document.CatalogueVersion,
            document.Occupations.Count,
            inserted,
            updated,
            deactivated);
    }
}
