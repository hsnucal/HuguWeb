using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class PreviewLeaveRequestUseCase(
    IWorkforceStore store,
    IWorkplaceContext workplaceContext,
    LeaveRequestComposer composer)
{
    public async Task<WorkforceResult<LeaveRequestPreviewDto>> ExecuteMineAsync(
        PreviewMyLeaveRequestCommand command,
        CancellationToken cancellationToken)
    {
        if (command.LinkedEmployeeId is null)
        {
            return WorkforceError.LeaveRequestAccountLinkRequired();
        }

        if (!workplaceContext.HasOrganization)
        {
            return WorkforceError.WorkplaceNotConfigured();
        }

        var employee = await store.GetEmployeeAsync(command.LinkedEmployeeId.Value, cancellationToken);
        if (employee is null || employee.OrganizationId != workplaceContext.OrganizationId)
        {
            return WorkforceError.LeaveRequestAccountLinkRequired();
        }

        var employments = await store.ListEmploymentsAsync(employee.Id, cancellationToken);
        var current = CurrentEmployment.Find(employments);
        if (!current.IsSuccess)
        {
            return WorkforceError.LeaveRequestCurrentEmploymentNotFound();
        }

        return await BuildPreviewAsync(
            current.Value!.Id,
            command.LeaveTypeId,
            command.StartDate,
            command.EndDate,
            command.RequestedAmount,
            cancellationToken);
    }

    public async Task<WorkforceResult<LeaveRequestPreviewDto>> ExecuteForEmploymentAsync(
        Guid employmentId,
        Guid? leaveTypeId,
        DateOnly startDate,
        DateOnly endDate,
        decimal? requestedAmount,
        CancellationToken cancellationToken) =>
        await BuildPreviewAsync(employmentId, leaveTypeId, startDate, endDate, requestedAmount, cancellationToken);

    private async Task<WorkforceResult<LeaveRequestPreviewDto>> BuildPreviewAsync(
        Guid employmentId,
        Guid? leaveTypeId,
        DateOnly startDate,
        DateOnly endDate,
        decimal? requestedAmount,
        CancellationToken cancellationToken)
    {
        if (startDate > endDate)
        {
            return WorkforceError.LeaveValidationField(
                LeaveValidation.Fields.EndDate,
                LeaveValidation.Codes.LeaveInvalidDateRange,
                "The leave start date cannot be after the end date.");
        }

        var preview = await composer.PreviewAsync(employmentId, startDate, endDate, cancellationToken);
        var warnings = new List<string>();
        if (preview.ScheduleIncomplete)
        {
            warnings.Add("leave-request-schedule-incomplete");
        }

        LeaveRequestBalanceWarningDto? balance = null;
        if (leaveTypeId is { } typeId)
        {
            var leaveType = await store.GetLeaveTypeAsync(typeId, cancellationToken);
            if (leaveType is not null && leaveType.TracksBalance && requestedAmount is { } amount)
            {
                balance = await composer.BuildBalanceAsync(
                    employmentId,
                    leaveType,
                    amount,
                    recordedAmountOverride: null,
                    cancellationToken);
                if (balance is { IsNegativeProjected: true })
                {
                    warnings.Add("leave-request-balance-overrun");
                }
            }
        }

        return new LeaveRequestPreviewDto(
            startDate,
            endDate,
            preview.SuggestedAmount,
            preview.ScheduleIncomplete,
            preview.Days.Select(day => new LeaveSchedulePreviewDayDto(day.Date, day.State, day.ChargeableCandidate))
                .ToArray(),
            balance,
            warnings);
    }
}

public sealed record PreviewMyLeaveRequestCommand(
    Guid? LinkedEmployeeId,
    Guid? LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal? RequestedAmount);
