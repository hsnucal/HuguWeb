using System.Text.Json;
using System.Text.Json.Serialization;

namespace HuGuWeb.Workforce.Domain;

public sealed record SgkOccupationCatalogueDocument(
    string Source,
    string CatalogueVersion,
    IReadOnlyList<SgkOccupationCatalogueRow> Occupations);

public sealed record SgkOccupationCatalogueRow(string Code, string Name, bool IsActive);

public static class SgkOccupationCatalogueParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static SgkOccupationCatalogueDocument Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Occupation catalogue JSON is empty.");
        }

        var payload = JsonSerializer.Deserialize<CatalogueFile>(json, Options)
            ?? throw new InvalidOperationException("Occupation catalogue JSON is invalid.");
        if (payload.Occupations is null || payload.Occupations.Count == 0)
        {
            throw new InvalidOperationException("Occupation catalogue does not contain any rows.");
        }

        var rows = new List<SgkOccupationCatalogueRow>(payload.Occupations.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in payload.Occupations)
        {
            var code = item.Code?.Trim() ?? string.Empty;
            var name = item.Name?.Trim() ?? string.Empty;
            if (!SgkOccupationCode.IsValidFormat(code))
            {
                throw new InvalidOperationException($"Occupation catalogue contains an invalid code '{code}'.");
            }

            if (name.Length == 0 || name.Length > SgkOccupationCode.DescriptionMaxLength)
            {
                throw new InvalidOperationException($"Occupation catalogue name for '{code}' is missing or too long.");
            }

            if (!seen.Add(code))
            {
                throw new InvalidOperationException($"Occupation catalogue contains duplicate code '{code}'.");
            }

            rows.Add(new SgkOccupationCatalogueRow(code, name, item.IsActive ?? true));
        }

        var source = string.IsNullOrWhiteSpace(payload.Source)
            ? "unknown"
            : payload.Source.Trim();
        var version = string.IsNullOrWhiteSpace(payload.CatalogueVersion)
            ? payload.ExtractedAt?.Trim() ?? "unknown"
            : payload.CatalogueVersion.Trim();
        return new SgkOccupationCatalogueDocument(source, version, rows);
    }

    private sealed class CatalogueFile
    {
        public string? Source { get; set; }
        public string? CatalogueVersion { get; set; }
        public string? ExtractedAt { get; set; }

        [JsonPropertyName("occupations")]
        public List<CatalogueRow>? Occupations { get; set; }
    }

    private sealed class CatalogueRow
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public bool? IsActive { get; set; }
    }
}
