namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// The ten organization-owned default leave types. <see cref="Code"/> and <see cref="SystemKind"/>
/// are the stable semantic identity; <see cref="DefaultName"/> is only the initial working name
/// (Turkish, matching existing seed data). UI labels are localized by <see cref="SystemKind"/>.
/// </summary>
public sealed record LeaveTypeDefault(
    string Code,
    LeaveTypeSystemKind SystemKind,
    bool TracksBalance,
    string DefaultName);

public static class LeaveTypeDefaults
{
    public static readonly IReadOnlyList<LeaveTypeDefault> All =
    [
        new("annual", LeaveTypeSystemKind.Annual, TracksBalance: true, "Yıllık İzin"),
        new("unpaid", LeaveTypeSystemKind.Unpaid, TracksBalance: false, "Ücretsiz İzin"),
        new("sick", LeaveTypeSystemKind.Sick, TracksBalance: false, "Hastalık İzni"),
        new("marriage", LeaveTypeSystemKind.Marriage, TracksBalance: false, "Evlilik İzni"),
        new("paternity", LeaveTypeSystemKind.Paternity, TracksBalance: false, "Babalık İzni"),
        new("maternity", LeaveTypeSystemKind.Maternity, TracksBalance: false, "Doğum İzni"),
        new("bereavement", LeaveTypeSystemKind.Bereavement, TracksBalance: false, "Ölüm İzni"),
        new("excuse", LeaveTypeSystemKind.Excuse, TracksBalance: false, "Mazeret İzni"),
        new("administrative", LeaveTypeSystemKind.Administrative, TracksBalance: false, "İdari İzin"),
        new("other", LeaveTypeSystemKind.Other, TracksBalance: false, "Diğer İzin"),
    ];

    /// <summary>
    /// Returns the default types whose code is not already present. Idempotent building block for
    /// organization initialization; never revives a hotel-deactivated system code (its code stays).
    /// </summary>
    public static IReadOnlyList<LeaveTypeDefault> Missing(IEnumerable<string> existingCodes)
    {
        var present = existingCodes
            .Select(LeaveType.NormalizeCodeForLookup)
            .ToHashSet(StringComparer.Ordinal);
        return All.Where(item => !present.Contains(item.Code)).ToArray();
    }
}

