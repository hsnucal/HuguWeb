namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// The ten organization-owned default leave types. <see cref="Code"/> and <see cref="SystemKind"/>
/// are the stable semantic identity; <see cref="DefaultName"/> is only the initial working name
/// (Turkish, matching existing seed data). UI labels are localized by <see cref="SystemKind"/>.
/// <see cref="DefaultRequestAmount"/> is optional request UX configuration only — not entitlement,
/// balance, or statutory calculation.
/// </summary>
public sealed record LeaveTypeDefault(
    string Code,
    LeaveTypeSystemKind SystemKind,
    bool TracksBalance,
    string DefaultName,
    decimal? DefaultRequestAmount = null);

public static class LeaveTypeDefaults
{
    public static readonly IReadOnlyList<LeaveTypeDefault> All =
    [
        new("annual", LeaveTypeSystemKind.Annual, TracksBalance: true, "Yıllık İzin"),
        new("unpaid", LeaveTypeSystemKind.Unpaid, TracksBalance: false, "Ücretsiz İzin"),
        new("sick", LeaveTypeSystemKind.Sick, TracksBalance: false, "Hastalık İzni"),
        new("marriage", LeaveTypeSystemKind.Marriage, TracksBalance: false, "Evlilik İzni"),
        new("paternity", LeaveTypeSystemKind.Paternity, TracksBalance: false, "Babalık İzni", 10.0m),
        new("maternity", LeaveTypeSystemKind.Maternity, TracksBalance: false, "Doğum İzni"),
        new("bereavement", LeaveTypeSystemKind.Bereavement, TracksBalance: false, "Ölüm İzni", 3.0m),
        new("excuse", LeaveTypeSystemKind.Excuse, TracksBalance: false, "Mazeret İzni"),
        new("administrative", LeaveTypeSystemKind.Administrative, TracksBalance: false, "İdari İzin"),
        new("other", LeaveTypeSystemKind.Other, TracksBalance: false, "Diğer İzin"),
    ];

    /// <summary>
    /// Optional custom leave type seed (no <see cref="LeaveTypeSystemKind"/>). Semantics come from
    /// persisted <c>DefaultRequestAmount</c>, never from the code string in UI logic.
    /// </summary>
    public static class OptionalCustom
    {
        public const string BirthdayCode = "birthday";
        public const string BirthdayDefaultName = "Doğum Günü İzni";
        public const decimal BirthdayDefaultRequestAmount = 1.0m;
    }

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
