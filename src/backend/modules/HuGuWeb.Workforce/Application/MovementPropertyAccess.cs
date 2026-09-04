using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

internal static class MovementPropertyAccess
{
    public static bool CanAccess(IReadOnlySet<Guid>? accessiblePropertyIds, Guid propertyId) =>
        accessiblePropertyIds is null || accessiblePropertyIds.Contains(propertyId);
}
