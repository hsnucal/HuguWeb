using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

/// <summary>
/// Cancels an Approved leave request and its linked LeaveRecord atomically.
/// Soft-cancels the record via HR-05A semantics; does not delete history.
/// </summary>
public sealed class CancelApprovedLeaveRequestUseCase(
    IWorkforceStore store,
    IWorkforceClock clock)
{
    public async Task<WorkforceResult<LeaveRequest>> ExecuteAsync(
        CancelApprovedLeaveRequestCommand command,
        CancellationToken cancellationToken)
    {
        var request = await store.GetLeaveRequestAsync(command.LeaveRequestId, cancellationToken);
        if (request is null)
        {
            return WorkforceError.LeaveRequestNotFound();
        }

        if (request.Status != LeaveRequestStatus.Approved)
        {
            return WorkforceError.LeaveRequestNotPending();
        }

        var record = await store.FindLeaveRecordBySourceLeaveRequestIdAsync(request.Id, cancellationToken);
        if (record is null || record.Status != LeaveRecordStatus.Recorded)
        {
            return WorkforceError.LeaveRequestRecordConflict();
        }

        await using var transaction = await store.BeginTransactionAsync(cancellationToken);
        try
        {
            if (!record.TryCancel(command.Reason, command.ActorUserId, clock.UtcNow, out var field, out var cancelError))
            {
                await transaction.RollbackAsync(cancellationToken);
                return cancelError switch
                {
                    LeaveValidation.Codes.LeaveAlreadyCancelled => WorkforceError.LeaveAlreadyCancelled(),
                    _ => WorkforceError.LeaveValidationField(
                        field ?? LeaveValidation.Fields.CancellationReason,
                        cancelError!,
                        "A cancellation reason is required.")
                };
            }

            if (!request.TryCancelApproved(
                    command.ActorUserId,
                    clock.UtcNow,
                    command.Reason,
                    out var decision,
                    out var requestError))
            {
                await transaction.RollbackAsync(cancellationToken);
                return WorkforceError.LeaveRequestNotPending();
            }

            store.AddLeaveRequestDecision(decision!);
            await store.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return request;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public sealed record CancelApprovedLeaveRequestCommand(
    Guid LeaveRequestId,
    string? Reason,
    string ActorUserId);
