namespace HuGuWeb.Workforce.Domain;

/// <summary>
/// Shared Primary Assignment resolution for a calendar date. No current/last/first fallbacks.
/// </summary>
public static class EffectiveAssignmentResolver
{
    public static Assignment? ResolvePrimaryAssignmentOnDate(
        IReadOnlyList<Assignment> assignments,
        DateOnly date) =>
        PrimaryAssignments.Covering(assignments, date);
}
