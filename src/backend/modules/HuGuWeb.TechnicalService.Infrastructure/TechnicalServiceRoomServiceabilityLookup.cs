using HuGuWeb.RoomOperations.Application;
using HuGuWeb.TechnicalService.Application;

namespace HuGuWeb.TechnicalService.Infrastructure;

public sealed class TechnicalServiceRoomServiceabilityLookup(ITechnicalServiceStore store) : IRoomServiceabilityLookup
{
    public async Task<IReadOnlyDictionary<Guid, RoomServiceabilitySnapshot>> GetForRoomsAsync(
        Guid propertyId,
        IReadOnlyCollection<Guid> roomIds,
        CancellationToken cancellationToken)
    {
        if (roomIds.Count == 0)
        {
            return new Dictionary<Guid, RoomServiceabilitySnapshot>();
        }

        var issues = propertyId == Guid.Empty
            ? []
            : await store.ListIssuesAsync(propertyId, cancellationToken);
        var byRoom = issues.ToLookup(issue => issue.RoomId);
        return roomIds
            .Distinct()
            .ToDictionary(
                roomId => roomId,
                roomId => Map(RoomServiceabilityProjection.Project(roomId, byRoom[roomId])));
    }

    private static RoomServiceabilitySnapshot Map(RoomServiceabilityView view) =>
        new(
            view.RoomId,
            view.Serviceability.ToString(),
            view.HasActiveTechnicalIssue,
            view.GoverningIssueId,
            view.GoverningIssueDescription);
}
