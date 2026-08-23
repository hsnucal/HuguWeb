namespace HuGuWeb.RoomOperations.Domain;

public sealed class RoomReadinessHistoryEntry
{
    private RoomReadinessHistoryEntry()
    {
        Comment = string.Empty;
    }

    private RoomReadinessHistoryEntry(
        Guid id,
        Guid roomId,
        Guid readinessCycleId,
        RoomReadiness readiness,
        ReadinessChangeCause cause,
        DateTimeOffset occurredAt,
        Guid? actorUserId,
        Guid? actorEmployeeId,
        Guid? workItemId,
        Guid? inspectionId,
        string? comment)
    {
        Id = id;
        RoomId = roomId;
        ReadinessCycleId = readinessCycleId;
        Readiness = readiness;
        Cause = cause;
        OccurredAt = occurredAt;
        ActorUserId = actorUserId;
        ActorEmployeeId = actorEmployeeId;
        WorkItemId = workItemId;
        InspectionId = inspectionId;
        Comment = comment;
    }

    public Guid Id { get; private set; }
    public Guid RoomId { get; private set; }
    public Guid ReadinessCycleId { get; private set; }
    public RoomReadiness Readiness { get; private set; }
    public ReadinessChangeCause Cause { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public Guid? ActorEmployeeId { get; private set; }
    public Guid? WorkItemId { get; private set; }
    public Guid? InspectionId { get; private set; }
    public string? Comment { get; private set; }

    public static RoomReadinessHistoryEntry Record(
        Guid id,
        Guid roomId,
        Guid readinessCycleId,
        RoomReadiness readiness,
        ReadinessChangeCause cause,
        DateTimeOffset occurredAt,
        Guid? actorUserId = null,
        Guid? actorEmployeeId = null,
        Guid? workItemId = null,
        Guid? inspectionId = null,
        string? comment = null)
    {
        return new RoomReadinessHistoryEntry(
            id,
            roomId,
            readinessCycleId,
            readiness,
            cause,
            occurredAt,
            actorUserId,
            actorEmployeeId,
            workItemId,
            inspectionId,
            string.IsNullOrWhiteSpace(comment) ? null : comment.Trim());
    }
}
