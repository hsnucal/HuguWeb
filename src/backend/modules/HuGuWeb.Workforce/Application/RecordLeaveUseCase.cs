using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class RecordLeaveUseCase(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext,
    EmployeeLeaveQuery leaveQuery)
{
    public async Task<WorkforceResult<EmployeeLeaveOverview>> ExecuteAsync(
        RecordLeaveCommand command,
        CancellationToken cancellationToken)
    {
        var context = await LeaveEmploymentContext.ResolveAsync(
            store,
            workplaceContext,
            command.EmployeeId,
            command.EmploymentId,
            cancellationToken);
        if (!context.IsSuccess)
        {
            return context.Error!;
        }

        var employment = context.Value.Employment;

        var leaveType = await store.GetLeaveTypeAsync(command.LeaveTypeId, cancellationToken);
        if (leaveType is null || leaveType.OrganizationId != context.Value.OrganizationId)
        {
            return WorkforceError.LeaveTypeNotFound();
        }

        if (!leaveType.IsActive)
        {
            return WorkforceError.LeaveTypeInactive();
        }

        if (!LeaveRecord.TryCreate(
                Guid.CreateVersion7(),
                employment.Id,
                leaveType.Id,
                command.StartDate,
                command.EndDate,
                command.Amount,
                command.Note,
                command.ActorUserId,
                clock.UtcNow,
                out var record,
                out var field,
                out var errorCode))
        {
            return WorkforceError.LeaveValidationField(field!, errorCode!, "The leave record is invalid.");
        }

        if (command.StartDate < employment.StartDate
            || (employment.EndDate is { } end && command.EndDate > end))
        {
            return WorkforceError.LeaveDateOutsideEmployment();
        }

        var existing = await store.ListLeaveRecordsAsync(employment.Id, cancellationToken);
        if (LeaveOverlap.OverlapsAnyRecorded(existing, command.StartDate, command.EndDate))
        {
            return WorkforceError.LeaveOverlap();
        }

        store.AddLeaveRecord(record!);
        await store.SaveChangesAsync(cancellationToken);

        return await leaveQuery.BuildAsync(context.Value.OrganizationId, employment, cancellationToken);
    }
}

public sealed record RecordLeaveCommand(
    Guid EmployeeId,
    Guid? EmploymentId,
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Amount,
    string? Note,
    string ActorUserId);
