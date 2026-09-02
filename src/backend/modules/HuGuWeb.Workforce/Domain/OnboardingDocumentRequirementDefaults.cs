namespace HuGuWeb.Workforce.Domain;

public sealed record OnboardingDocumentRequirementDefault(
    string Code,
    string DefaultName,
    int SortOrder,
    bool IsRequiredByDefault = true);

public static class OnboardingDocumentRequirementDefaults
{
    public static readonly IReadOnlyList<OnboardingDocumentRequirementDefault> All =
    [
        new("ID_COPY", "Kimlik Fotokopisi", 1),
        new("RESIDENCE", "İkametgâh Belgesi", 2),
        new("CRIMINAL_RECORD", "Adli Sicil Belgesi", 3),
        new("DIPLOMA", "Diploma / Mezuniyet Belgesi", 4),
        new("HEALTH_REPORT", "Sağlık Raporu", 5),
        new("PHOTO", "Vesikalık Fotoğraf", 6),
        new("BANK_IBAN", "Banka / IBAN Bilgisi", 7)
    ];

    public static IReadOnlyList<OnboardingDocumentRequirementDefault> Missing(IEnumerable<string> existingCodes)
    {
        var present = existingCodes
            .Select(OnboardingDocumentRequirement.NormalizeCodeForLookup)
            .ToHashSet(StringComparer.Ordinal);
        return All.Where(item => !present.Contains(item.Code)).ToArray();
    }
}
