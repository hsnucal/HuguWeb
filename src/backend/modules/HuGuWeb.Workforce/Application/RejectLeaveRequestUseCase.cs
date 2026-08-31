using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class RejectLeaveRequestUseCase(
    IWorkforceStore store,
    IWorkforceClock clock)
{
    public async Task<WorkforceResult<LeaveRequest>> ExecuteAsync(
        RejectLeaveRequestCommand command,
        CancellationToken cancellationToken)
    {
        var request = await store.GetLeaveRequestAsync(command.LeaveRequestId, cancellationToken);
        if (request is null)
        {
            return WorkforceError.LeaveRequestNotFound();
        }

        if (!request.TryReject(command.ActorUserId, clock.UtcNow, command.Note, out var decision, out var errorCode))
        {
            return errorCode switch
            {
                LeaveValidation.Codes.LeaveRequestAlreadyFinalized =>
                    WorkforceError.LeaveRequestAlreadyFinalized(),
                LeaveValidation.Codes.LeaveRequestInvalidApprovalStage =>
                    WorkforceError.LeaveRequestInvalidApprovalStage(),
                _ => WorkforceError.LeaveRequestNotPending()
            };
        }

        store.AddLeaveRequestDecision(decision!);
        await store.SaveChangesAsync(cancellationToken);
        return request;
    }
}

public sealed record RejectLeaveRequestCommand(Guid LeaveRequestId, string? Note, string ActorUserId);
