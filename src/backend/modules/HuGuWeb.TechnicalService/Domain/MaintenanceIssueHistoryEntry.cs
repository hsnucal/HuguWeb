namespace HuGuWeb.TechnicalService.Domain;

public sealed class MaintenanceIssueHistoryEntry
{
    public const int NoteMaxLength = MaintenanceIssue.NoteMaxLength;

    private MaintenanceIssueHistoryEntry()
    {
    }

    private MaintenanceIssueHistoryEntry(
        Guid id,
        Guid issueId,
        DateTimeOffset occurredAt,
        Guid actingUserId,
        MaintenanceIssueHistoryEvent eventType,
        MaintenanceIssueStatus? fromStatus,
        MaintenanceIssueStatus? toStatus,
        Guid? fromEmployeeId,
        Guid? toEmployeeId,
        MaintenancePriority? fromPriority,
        MaintenancePriority? toPriority,
        bool? blocksRoomUse,
        OutageClassification? outageClassification,
        PreparationImpact? preparationImpact,
        string? note)
    {
        Id = id;
        IssueId = issueId;
        OccurredAt = occurredAt;
        ActingUserId = actingUserId;
        EventType = eventType;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        FromEmployeeId = fromEmployeeId;
        ToEmployeeId = toEmployeeId;
        FromPriority = fromPriority;
        ToPriority = toPriority;
        BlocksRoomUse = blocksRoomUse;
        OutageClassification = outageClassification;
        PreparationImpact = preparationImpact;
        Note = note;
    }

    public Guid Id { get; private set; }
    public Guid IssueId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public Guid ActingUserId { get; private set; }
    public MaintenanceIssueHistoryEvent EventType { get; private set; }
    public MaintenanceIssueStatus? FromStatus { get; private set; }
    public MaintenanceIssueStatus? ToStatus { get; private set; }
    public Guid? FromEmployeeId { get; private set; }
    public Guid? ToEmployeeId { get; private set; }
    public MaintenancePriority? FromPriority { get; private set; }
    public MaintenancePriority? ToPriority { get; private set; }
    public bool? BlocksRoomUse { get; private set; }
    public OutageClassification? OutageClassification { get; private set; }
    public PreparationImpact? PreparationImpact { get; private set; }
    public string? Note { get; private set; }

    public static MaintenanceIssueHistoryEntry Record(
        Guid id,
        Guid issueId,
        DateTimeOffset occurredAt,
        Guid actingUserId,
        MaintenanceIssueHistoryEvent eventType,
        MaintenanceIssueStatus? fromStatus = null,
        MaintenanceIssueStatus? toStatus = null,
        Guid? fromEmployeeId = null,
        Guid? toEmployeeId = null,
        MaintenancePriority? fromPriority = null,
        MaintenancePriority? toPriority = null,
        bool? blocksRoomUse = null,
        OutageClassification? outageClassification = null,
        PreparationImpact? preparationImpact = null,
        string? note = null)
    {
        return new MaintenanceIssueHistoryEntry(
            id,
            issueId,
            occurredAt,
            actingUserId,
            eventType,
            fromStatus,
            toStatus,
            fromEmployeeId,
            toEmployeeId,
            fromPriority,
            toPriority,
            blocksRoomUse,
            outageClassification,
            preparationImpact,
            string.IsNullOrWhiteSpace(note) ? null : note.Trim());
    }
}
