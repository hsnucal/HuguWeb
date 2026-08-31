using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed record LeaveRequestListItemDto(
    Guid Id,
    Guid EmploymentId,
    Guid EmployeeId,
    string PersonnelNumber,
    string DisplayName,
    Guid AssignmentId,
    Guid DepartmentId,
    string DepartmentName,
    Guid LeaveTypeId,
    string LeaveTypeCode,
    string LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal RequestedAmount,
    decimal? FinalAmount,
    LeaveRequestStatus Status,
    LeaveRequestApprovalStage ApprovalStage,
    string? Reason,
    DateTimeOffset CreatedAtUtc,
    bool ScheduleIncomplete);

public sealed record LeaveRequestDecisionDto(
    Guid Id,
    LeaveRequestApprovalStage Stage,
    LeaveRequestDecisionKind Decision,
    string ActorUserId,
    DateTimeOffset DecisionAtUtc,
    string? Note);

public sealed record LeaveRequestLinkedRecordDto(
    Guid Id,
    decimal Amount,
    LeaveRecordStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string? CancellationReason);

public sealed record LeaveRequestBalanceWarningDto(
    Guid LeaveTypeId,
    string LeaveTypeCode,
    decimal CurrentBalance,
    decimal ProjectedBalance,
    bool IsNegativeProjected);

public sealed record LeaveRequestDetailDto(
    Guid Id,
    Guid EmploymentId,
    Guid EmployeeId,
    string PersonnelNumber,
    string DisplayName,
    Guid AssignmentId,
    Guid DepartmentId,
    string DepartmentName,
    Guid? PositionId,
    string? PositionName,
    Guid PropertyId,
    Guid LeaveTypeId,
    string LeaveTypeCode,
    string LeaveTypeName,
    bool TracksBalance,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal RequestedAmount,
    decimal? FinalAmount,
    decimal SuggestedAmount,
    bool ScheduleIncomplete,
    LeaveRequestStatus Status,
    LeaveRequestApprovalStage ApprovalStage,
    string? Reason,
    string CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<LeaveSchedulePreviewDayDto> ScheduleDays,
    IReadOnlyList<LeaveRequestDecisionDto> Decisions,
    LeaveRequestLinkedRecordDto? LinkedRecord,
    LeaveRequestBalanceWarningDto? Balance,
    IReadOnlyList<string> Warnings);

public sealed record LeaveSchedulePreviewDayDto(DateOnly Date, string State, decimal ChargeableCandidate);

public sealed record LeaveRequestPreviewDto(
    DateOnly StartDate,
    DateOnly EndDate,
    decimal SuggestedAmount,
    bool ScheduleIncomplete,
    IReadOnlyList<LeaveSchedulePreviewDayDto> Days,
    LeaveRequestBalanceWarningDto? Balance,
    IReadOnlyList<string> Warnings);

public sealed record LeaveRequestMutationResultDto(
    LeaveRequestDetailDto Request,
    IReadOnlyList<string> Warnings);

public sealed record LeaveRequestListPageDto(
    IReadOnlyList<LeaveRequestListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
