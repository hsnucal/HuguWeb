using HuGuWeb.TechnicalService.Domain;

namespace HuGuWeb.TechnicalService.Application;

public sealed record RoomServiceabilityView(
    Guid RoomId,
    RoomServiceabilityState Serviceability,
    bool HasActiveTechnicalIssue,
    Guid? GoverningIssueId,
    string? GoverningIssueDescription);

public static class RoomServiceabilityProjection
{
    public static RoomServiceabilityView Project(Guid roomId, IEnumerable<MaintenanceIssue> issues)
    {
        var current = issues.ToArray();
        var serviceability = RoomServiceability.Derive(current);
        var governing = RoomServiceability.GoverningIssue(current);
        return new RoomServiceabilityView(
            roomId,
            serviceability,
            HasActiveTechnicalIssue: serviceability != RoomServiceabilityState.Serviceable,
            governing?.Id,
            governing?.Description);
    }
}
