using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class CancelLeaveRecordUseCase(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext,
    EmployeeLeaveQuery leaveQuery)
{
    public async Task<WorkforceResult<EmployeeLeaveOverview>> ExecuteAsync(
        CancelLeaveRecordCommand command,
        CancellationToken cancellationToken)
    {
        var context = await LeaveEmploymentContext.ResolveAsync(
            store,
            workplaceContext,
            command.EmployeeId,
            employmentId: null,
            cancellationToken);
        if (!context.IsSuccess)
        {
            return context.Error!;
        }

        var record = await store.GetLeaveRecordAsync(command.RecordId, cancellationToken);
        if (record is null)
        {
            return WorkforceError.LeaveRecordNotFound();
        }

        var employments = await store.ListEmploymentsAsync(command.EmployeeId, cancellationToken);
        var employment = employments.FirstOrDefault(item => item.Id == record.EmploymentId);
        if (employment is null)
        {
            return WorkforceError.LeaveRecordNotFound();
        }

        if (!record.TryCancel(command.Reason, command.ActorUserId, clock.UtcNow, out var field, out var errorCode))
        {
            return errorCode switch
            {
                LeaveValidation.Codes.LeaveAlreadyCancelled => WorkforceError.LeaveAlreadyCancelled(),
                _ => WorkforceError.LeaveValidationField(
                    field ?? LeaveValidation.Fields.CancellationReason,
                    errorCode!,
                    "A cancellation reason is required.")
            };
        }

        await store.SaveChangesAsync(cancellationToken);

        return await leaveQuery.BuildAsync(context.Value.OrganizationId, employment, cancellationToken);
    }
}

public sealed record CancelLeaveRecordCommand(
    Guid EmployeeId,
    Guid RecordId,
    string? Reason,
    string ActorUserId);
