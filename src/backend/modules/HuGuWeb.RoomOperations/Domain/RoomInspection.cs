namespace HuGuWeb.RoomOperations.Domain;

public sealed class RoomInspection
{
    public const int ReasonMaxLength = 500;

    private RoomInspection()
    {
        Reason = string.Empty;
    }

    private RoomInspection(
        Guid id,
        Guid roomId,
        Guid readinessCycleId,
        Guid inspectorUserId,
        InspectionResult result,
        string? reason,
        DateTimeOffset occurredAt,
        Guid? workItemId)
    {
        Id = id;
        RoomId = roomId;
        ReadinessCycleId = readinessCycleId;
        InspectorUserId = inspectorUserId;
        Result = result;
        Reason = reason;
        OccurredAt = occurredAt;
        WorkItemId = workItemId;
    }

    public Guid Id { get; private set; }
    public Guid RoomId { get; private set; }
    public Guid ReadinessCycleId { get; private set; }
    public Guid InspectorUserId { get; private set; }
    public InspectionResult Result { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid? WorkItemId { get; private set; }

    public static bool TryAccept(
        Guid id,
        Guid roomId,
        Guid readinessCycleId,
        Guid inspectorUserId,
        DateTimeOffset occurredAt,
        Guid? workItemId,
        out RoomInspection? inspection,
        out string? error)
    {
        return TryCreate(
            id,
            roomId,
            readinessCycleId,
            inspectorUserId,
            InspectionResult.Accepted,
            reason: null,
            occurredAt,
            workItemId,
            out inspection,
            out error);
    }

    public static bool TryReject(
        Guid id,
        Guid roomId,
        Guid readinessCycleId,
        Guid inspectorUserId,
        string? reason,
        DateTimeOffset occurredAt,
        Guid? workItemId,
        out RoomInspection? inspection,
        out string? error)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            inspection = null;
            error = "A rejection reason is required.";
            return false;
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > ReasonMaxLength)
        {
            inspection = null;
            error = $"Rejection reason must be {ReasonMaxLength} characters or fewer.";
            return false;
        }

        return TryCreate(
            id,
            roomId,
            readinessCycleId,
            inspectorUserId,
            InspectionResult.Rejected,
            trimmed,
            occurredAt,
            workItemId,
            out inspection,
            out error);
    }

    private static bool TryCreate(
        Guid id,
        Guid roomId,
        Guid readinessCycleId,
        Guid inspectorUserId,
        InspectionResult result,
        string? reason,
        DateTimeOffset occurredAt,
        Guid? workItemId,
        out RoomInspection? inspection,
        out string? error)
    {
        inspection = null;
        if (id == Guid.Empty || roomId == Guid.Empty || readinessCycleId == Guid.Empty || inspectorUserId == Guid.Empty)
        {
            error = "Inspection identity is invalid.";
            return false;
        }

        inspection = new RoomInspection(
            id,
            roomId,
            readinessCycleId,
            inspectorUserId,
            result,
            reason,
            occurredAt,
            workItemId);
        error = null;
        return true;
    }
}
