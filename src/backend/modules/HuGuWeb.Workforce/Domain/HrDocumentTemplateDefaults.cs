namespace HuGuWeb.Workforce.Domain;

public sealed record HrDocumentTemplateDefault(
    string Code,
    string DefaultName,
    string? Description,
    HrDocumentTemplateCategory Category,
    string Content,
    string Version,
    int SortOrder,
    string? TemplateAssetPath);

public static class HrDocumentTemplateDefaults
{
    public const string OvertimeConsentCode = "OVERTIME-CONSENT";
    public const string OvertimeConsentAssetPath = "Templates/Onboarding/Taslak.docx";

    /// <summary>
    /// HTML fallback when no DOCX asset is configured. OVERTIME-CONSENT previews use the embedded DOCX.
    /// </summary>
    public static readonly string OvertimeConsentPlaceholderContent =
        """
        <p>Fazla Çalışma Yapmaya-Serbest Zaman Olarak Kullanmaya-Hafta ve Genel Tatil Günlerinde Çalışma Yapmaya Muvafakat Belgesi</p>
        <p>Giriş Tarihi: {{Employment.StartDate}}</p>
        <p>İşçi Adı Soyadı: {{Employee.FullName}}</p>
        <p>İmza: ________________</p>
        """;

    public static readonly IReadOnlyList<HrDocumentTemplateDefault> All =
    [
        new(
            OvertimeConsentCode,
            "Fazla Çalışma Muvafakat Belgesi",
            "Overtime consent; DOCX asset Templates/Onboarding/Taslak.docx (from PO Taslak.docx).",
            HrDocumentTemplateCategory.Onboarding,
            OvertimeConsentPlaceholderContent,
            "1",
            1,
            OvertimeConsentAssetPath)
    ];

    public static IReadOnlyList<HrDocumentTemplateDefault> Missing(IEnumerable<string> existingCodes)
    {
        var present = existingCodes
            .Select(HrDocumentTemplate.NormalizeCodeForLookup)
            .ToHashSet(StringComparer.Ordinal);
        return All.Where(item => !present.Contains(item.Code)).ToArray();
    }
}
