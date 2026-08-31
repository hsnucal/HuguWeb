using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class LeaveRequestComposer(IWorkforceStore store)
{
    public async Task<LeaveSchedulePreviewResult> PreviewAsync(
        Guid employmentId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var entries = await store.ListScheduleEntriesAsync(
            [employmentId],
            startDate,
            endDate,
            cancellationToken);
        return LeaveSchedulePreview.Build(startDate, endDate, entries);
    }

    public async Task<LeaveRequestDetailDto?> ComposeDetailAsync(
        LeaveRequest request,
        CancellationToken cancellationToken)
    {
        var employment = await store.GetEmploymentAsync(request.EmploymentId, cancellationToken);
        if (employment is null)
        {
            return null;
        }

        var employee = await store.GetEmployeeAsync(employment.EmployeeId, cancellationToken);
        var assignment = await store.GetAssignmentAsync(request.AssignmentId, cancellationToken);
        if (employee is null || assignment is null)
        {
            return null;
        }

        var department = await store.GetDepartmentAsync(assignment.DepartmentId, cancellationToken);
        var position = await store.GetPositionAsync(assignment.PositionId, cancellationToken);
        var leaveType = await store.GetLeaveTypeAsync(request.LeaveTypeId, cancellationToken);
        if (department is null || leaveType is null)
        {
            return null;
        }

        var decisions = await store.ListLeaveRequestDecisionsAsync(request.Id, cancellationToken);
        var linked = await store.FindLeaveRecordBySourceLeaveRequestIdAsync(request.Id, cancellationToken);
        var preview = await PreviewAsync(request.EmploymentId, request.StartDate, request.EndDate, cancellationToken);

        LeaveRequestBalanceWarningDto? balance = null;
        var warnings = new List<string>();
        if (leaveType.TracksBalance)
        {
            balance = await BuildBalanceAsync(
                employment.Id,
                leaveType,
                request.RequestedAmount,
                linked?.Status == LeaveRecordStatus.Recorded ? linked.Amount : null,
                cancellationToken);
            if (balance is { IsNegativeProjected: true })
            {
                warnings.Add("leave-request-balance-overrun");
            }
        }

        if (preview.ScheduleIncomplete)
        {
            warnings.Add("leave-request-schedule-incomplete");
        }

        return new LeaveRequestDetailDto(
            request.Id,
            request.EmploymentId,
            employee.Id,
            employee.PersonnelNumber,
            DisplayName(employee),
            request.AssignmentId,
            department.Id,
            department.Name,
            position?.Id,
            position?.Name,
            department.PropertyId,
            leaveType.Id,
            leaveType.Code,
            leaveType.Name,
            leaveType.TracksBalance,
            request.StartDate,
            request.EndDate,
            request.RequestedAmount,
            linked is { Status: LeaveRecordStatus.Recorded or LeaveRecordStatus.Cancelled }
                ? linked.Amount
                : null,
            preview.SuggestedAmount,
            preview.ScheduleIncomplete,
            request.Status,
            request.ApprovalStage,
            request.Reason,
            request.CreatedByUserId,
            request.CreatedAtUtc,
            request.UpdatedAtUtc,
            preview.Days.Select(day => new LeaveSchedulePreviewDayDto(day.Date, day.State, day.ChargeableCandidate))
                .ToArray(),
            decisions.Select(item => new LeaveRequestDecisionDto(
                    item.Id,
                    item.Stage,
                    item.Decision,
                    item.ActorUserId,
                    item.DecisionAtUtc,
                    item.Note))
                .ToArray(),
            linked is null
                ? null
                : new LeaveRequestLinkedRecordDto(
                    linked.Id,
                    linked.Amount,
                    linked.Status,
                    linked.CreatedAtUtc,
                    linked.CancelledAtUtc,
                    linked.CancellationReason),
            balance,
            warnings);
    }

    public async Task<LeaveRequestListItemDto?> ComposeListItemAsync(
        LeaveRequest request,
        CancellationToken cancellationToken)
    {
        var detail = await ComposeDetailAsync(request, cancellationToken);
        if (detail is null)
        {
            return null;
        }

        return new LeaveRequestListItemDto(
            detail.Id,
            detail.EmploymentId,
            detail.EmployeeId,
            detail.PersonnelNumber,
            detail.DisplayName,
            detail.AssignmentId,
            detail.DepartmentId,
            detail.DepartmentName,
            detail.LeaveTypeId,
            detail.LeaveTypeCode,
            detail.LeaveTypeName,
            detail.StartDate,
            detail.EndDate,
            detail.RequestedAmount,
            detail.FinalAmount,
            detail.Status,
            detail.ApprovalStage,
            detail.Reason,
            detail.CreatedAtUtc,
            detail.ScheduleIncomplete);
    }

    public async Task<LeaveRequestBalanceWarningDto?> BuildBalanceAsync(
        Guid employmentId,
        LeaveType leaveType,
        decimal amountForProjection,
        decimal? recordedAmountOverride,
        CancellationToken cancellationToken)
    {
        if (!leaveType.TracksBalance)
        {
            return null;
        }

        var entitlements = await store.ListLeaveEntitlementsAsync(employmentId, cancellationToken);
        var records = await store.ListLeaveRecordsAsync(employmentId, cancellationToken);
        var net = entitlements.Where(item => item.LeaveTypeId == leaveType.Id).Sum(item => item.Amount);
        var used = records
            .Where(item => item.LeaveTypeId == leaveType.Id && item.Status == LeaveRecordStatus.Recorded)
            .Sum(item => item.Amount);
        var current = net - used;
        var deduct = recordedAmountOverride ?? amountForProjection;
        var projected = current - deduct;
        return new LeaveRequestBalanceWarningDto(
            leaveType.Id,
            leaveType.Code,
            current,
            projected,
            projected < 0m);
    }

    public static string DisplayName(Employee employee) =>
        $"{employee.GivenName} {employee.FamilyName}".Trim();
}
