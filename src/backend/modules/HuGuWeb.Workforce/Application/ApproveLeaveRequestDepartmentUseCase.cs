using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class ApproveLeaveRequestDepartmentUseCase(
    IWorkforceStore store,
    IWorkforceClock clock)
{
    public async Task<WorkforceResult<LeaveRequest>> ExecuteAsync(
        ApproveLeaveRequestDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        var request = await store.GetLeaveRequestAsync(command.LeaveRequestId, cancellationToken);
        if (request is null)
        {
            return WorkforceError.LeaveRequestNotFound();
        }

        if (!request.TryApproveDepartment(command.ActorUserId, clock.UtcNow, out var decision, out var errorCode))
        {
            return MapTransitionError(errorCode!);
        }

        store.AddLeaveRequestDecision(decision!);
        await store.SaveChangesAsync(cancellationToken);
        return request;
    }

    private static WorkforceError MapTransitionError(string errorCode) =>
        errorCode switch
        {
            LeaveValidation.Codes.LeaveRequestAlreadyFinalized => WorkforceError.LeaveRequestAlreadyFinalized(),
            LeaveValidation.Codes.LeaveRequestInvalidApprovalStage =>
                WorkforceError.LeaveRequestInvalidApprovalStage(),
            _ => WorkforceError.LeaveRequestNotPending()
        };
}

public sealed record ApproveLeaveRequestDepartmentCommand(Guid LeaveRequestId, string ActorUserId);
