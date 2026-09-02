namespace HuGuWeb.Workforce.Domain;

public sealed record RecruitmentSourceDefault(string Code, string DefaultName, int SortOrder);

public static class RecruitmentSourceDefaults
{
    public static readonly IReadOnlyList<RecruitmentSourceDefault> All =
    [
        new("LINKEDIN", "LinkedIn", 1),
        new("KARIYER_NET", "Kariyer.net", 2),
        new("YENIBIRIS", "yenibiris.com", 3),
        new("DIRECT_APPLICATION", "Direkt Başvuru", 4),
        new("REFERRAL", "Referans", 5)
    ];

    public static IReadOnlyList<RecruitmentSourceDefault> Missing(IEnumerable<string> existingCodes)
    {
        var present = existingCodes
            .Select(RecruitmentSource.NormalizeCodeForLookup)
            .ToHashSet(StringComparer.Ordinal);
        return All.Where(item => !present.Contains(item.Code)).ToArray();
    }
}
