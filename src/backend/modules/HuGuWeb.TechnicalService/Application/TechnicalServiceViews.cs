using HuGuWeb.TechnicalService.Domain;

namespace HuGuWeb.TechnicalService.Application;

public sealed record MaintenanceIssueListItem(
    Guid Id,
    Guid RoomId,
    string RoomNumber,
    string Description,
    Guid CategoryId,
    string CategoryName,
    MaintenancePriority Priority,
    MaintenanceIssueStatus Status,
    Guid? AssignedEmployeeId,
    string? AssignedEmployeeName,
    bool BlocksRoomUse,
    OutageClassification? OutageClassification,
    RoomServiceabilityState RoomServiceability,
    DateTimeOffset CreatedAt,
    int Version,
    string NeededAction);

public sealed record MaintenanceIssueDetail(
    Guid Id,
    Guid RoomId,
    string RoomNumber,
    string Description,
    Guid CategoryId,
    string CategoryName,
    MaintenancePriority Priority,
    MaintenanceIssueStatus Status,
    Guid? AssignedEmployeeId,
    string? AssignedEmployeeName,
    Guid? ReportedByEmployeeId,
    string? ReportedByEmployeeName,
    string? OriginNote,
    bool BlocksRoomUse,
    OutageClassification? OutageClassification,
    RoomServiceabilityState RoomServiceability,
    string? ResolutionNote,
    string? UnableToResolveNote,
    PreparationImpact? PreparationImpact,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? ResolvedAt,
    int Version,
    string NeededAction,
    IReadOnlyList<MaintenanceIssueHistoryItem> History);

public sealed record MaintenanceIssueHistoryItem(
    Guid Id,
    MaintenanceIssueHistoryEvent EventType,
    DateTimeOffset OccurredAt,
    MaintenanceIssueStatus? FromStatus,
    MaintenanceIssueStatus? ToStatus,
    Guid? FromEmployeeId,
    string? FromEmployeeName,
    Guid? ToEmployeeId,
    string? ToEmployeeName,
    MaintenancePriority? FromPriority,
    MaintenancePriority? ToPriority,
    bool? BlocksRoomUse,
    OutageClassification? OutageClassification,
    PreparationImpact? PreparationImpact,
    string? Note);

public sealed record AssignableEmployeeItem(
    Guid EmployeeId,
    string GivenName,
    string FamilyName,
    string PersonnelNumber,
    string DisplayName);

public sealed record MaintenanceRoomItem(Guid RoomId, string Number);

public sealed record MaintenanceCategoryItem(Guid Id, string Name);
