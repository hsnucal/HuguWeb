using HuGuWeb.RoomOperations.Domain;

namespace HuGuWeb.RoomOperations.Application;

public sealed record RoomOperationsListItem(
    Guid Id,
    string Number,
    bool IsActive,
    RoomReadiness Readiness,
    Guid ReadinessCycleId,
    Guid? CurrentWorkItemId,
    HousekeepingWorkState? CurrentWorkState,
    HousekeepingWorkOrigin? CurrentWorkOrigin,
    TaskPriority? Priority,
    Guid? AssignedEmployeeId,
    string? AssignedEmployeeName,
    string NeededAction);

public sealed record RoomOperationsDetail(
    Guid Id,
    string Number,
    bool IsActive,
    RoomReadiness Readiness,
    Guid ReadinessCycleId,
    HousekeepingWorkSummary? CurrentWork,
    IReadOnlyList<ReadinessHistoryItem> ReadinessHistory,
    IReadOnlyList<InspectionHistoryItem> InspectionHistory);

public sealed record HousekeepingWorkSummary(
    Guid Id,
    HousekeepingWorkState State,
    HousekeepingWorkOrigin Origin,
    TaskPriority Priority,
    Guid AssignedEmployeeId,
    string AssignedEmployeeName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    Guid? CompletedByEmployeeId,
    Guid ReadinessCycleId,
    Guid? SourceInspectionId);

public sealed record ReadinessHistoryItem(
    Guid Id,
    RoomReadiness Readiness,
    ReadinessChangeCause Cause,
    DateTimeOffset OccurredAt,
    Guid? ActorEmployeeId,
    string? ActorEmployeeName,
    Guid? WorkItemId,
    Guid? InspectionId,
    string? Comment);

public sealed record InspectionHistoryItem(
    Guid Id,
    InspectionResult Result,
    DateTimeOffset OccurredAt,
    Guid InspectorUserId,
    string? Reason,
    Guid ReadinessCycleId,
    Guid? WorkItemId);

public sealed record AssignableEmployeeItem(
    Guid EmployeeId,
    string GivenName,
    string FamilyName,
    string PersonnelNumber,
    string DisplayName);
