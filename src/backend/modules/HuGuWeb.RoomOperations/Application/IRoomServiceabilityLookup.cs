namespace HuGuWeb.RoomOperations.Application;

public sealed record RoomServiceabilitySnapshot(
    Guid RoomId,
    string Serviceability,
    bool HasActiveTechnicalIssue,
    Guid? GoverningIssueId,
    string? GoverningIssueDescription)
{
    public const string Serviceable = "Serviceable";

    public static RoomServiceabilitySnapshot Available(Guid roomId) =>
        new(roomId, Serviceable, false, null, null);
}

public interface IRoomServiceabilityLookup
{
    Task<IReadOnlyDictionary<Guid, RoomServiceabilitySnapshot>> GetForRoomsAsync(
        Guid propertyId,
        IReadOnlyCollection<Guid> roomIds,
        CancellationToken cancellationToken);
}
