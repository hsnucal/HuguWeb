using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class RecordLeaveEntitlementUseCase(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext,
    EmployeeLeaveQuery leaveQuery)
{
    public async Task<WorkforceResult<EmployeeLeaveOverview>> ExecuteAsync(
        RecordLeaveEntitlementCommand command,
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

        var leaveType = await store.GetLeaveTypeAsync(command.LeaveTypeId, cancellationToken);
        if (leaveType is null || leaveType.OrganizationId != context.Value.OrganizationId)
        {
            return WorkforceError.LeaveTypeNotFound();
        }

        if (!leaveType.IsActive)
        {
            return WorkforceError.LeaveTypeInactive();
        }

        if (!leaveType.TracksBalance)
        {
            return WorkforceError.LeaveEntitlementBalanceNotSupported();
        }

        if (!LeaveEntitlement.TryCreate(
                Guid.CreateVersion7(),
                context.Value.Employment.Id,
                leaveType.Id,
                command.EffectiveDate,
                command.Amount,
                command.Source,
                command.Note,
                command.ActorUserId,
                clock.UtcNow,
                out var entitlement,
                out var field,
                out var errorCode))
        {
            return WorkforceError.LeaveValidationField(field!, errorCode!, "The entitlement movement is invalid.");
        }

        store.AddLeaveEntitlement(entitlement!);
        await store.SaveChangesAsync(cancellationToken);

        return await leaveQuery.BuildAsync(context.Value.OrganizationId, context.Value.Employment, cancellationToken);
    }
}

public sealed record RecordLeaveEntitlementCommand(
    Guid EmployeeId,
    Guid? EmploymentId,
    Guid LeaveTypeId,
    DateOnly EffectiveDate,
    decimal Amount,
    LeaveEntitlementSource Source,
    string? Note,
    string ActorUserId);
