namespace HuGuWeb.TechnicalService.Domain;

public enum MaintenanceIssueHistoryEvent
{
    Created = 0,
    Assigned = 1,
    Reassigned = 2,
    PriorityChanged = 3,
    BlockingChanged = 4,
    Started = 5,
    UnableToResolve = 6,
    Resumed = 7,
    Resolved = 8
}
