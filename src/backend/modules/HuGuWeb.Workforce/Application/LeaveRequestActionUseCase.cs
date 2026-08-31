using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class LeaveRequestActionUseCase(
    LeaveRequestQuery query,
    LeaveRequestComposer composer,
    ApproveLeaveRequestDepartmentUseCase departmentApprove,
    ApproveLeaveRequestHrUseCase hrApprove,
    RejectLeaveRequestUseCase reject,
    WithdrawLeaveRequestUseCase withdraw,
    CancelApprovedLeaveRequestUseCase cancelApproved)
{
    public async Task<WorkforceResult<LeaveRequestDetailDto>> WithdrawMineAsync(
        Guid linkedEmployeeId,
        Guid leaveRequestId,
        string? note,
        string actorUserId,
        CancellationToken cancellationToken)
    {
        var owned = await query.GetMineAsync(linkedEmployeeId, leaveRequestId, cancellationToken);
        if (!owned.IsSuccess)
        {
            return owned.Error!;
        }

        var result = await withdraw.ExecuteAsync(
            new WithdrawLeaveRequestCommand(leaveRequestId, note, actorUserId),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        var detail = await composer.ComposeDetailAsync(result.Value!, cancellationToken);
        return detail is null ? WorkforceError.LeaveRequestNotFound() : detail;
    }

    public async Task<WorkforceResult<LeaveRequestMutationResultDto>> DepartmentApproveAsync(
        Guid leaveRequestId,
        string? note,
        string actorUserId,
        Guid? scopedPropertyId,
        IReadOnlySet<Guid>? allowedDepartmentIds,
        bool canApprove,
        CancellationToken cancellationToken)
    {
        if (!canApprove)
        {
            return WorkforceError.LeaveRequestApprovalPermissionDenied();
        }

        var access = await query.ResolveAccessibleRequestAsync(
            leaveRequestId,
            scopedPropertyId,
            allowedDepartmentIds,
            cancellationToken);
        if (!access.IsSuccess)
        {
            return access.Error!;
        }

        if (access.Value.ApprovalStage != LeaveRequestApprovalStage.Department)
        {
            return WorkforceError.LeaveRequestInvalidApprovalStage();
        }

        var result = await departmentApprove.ExecuteAsync(
            new ApproveLeaveRequestDepartmentCommand(leaveRequestId, actorUserId),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        // optional note: department approve core has no note; append is only via decision without note in Slice A
        _ = note;

        return await MutationResultAsync(result.Value!, cancellationToken);
    }

    public async Task<WorkforceResult<LeaveRequestMutationResultDto>> RejectAsync(
        Guid leaveRequestId,
        string? note,
        string actorUserId,
        Guid? scopedPropertyId,
        IReadOnlySet<Guid>? allowedDepartmentIds,
        bool canApprove,
        bool canManage,
        CancellationToken cancellationToken)
    {
        var access = await query.ResolveAccessibleRequestAsync(
            leaveRequestId,
            scopedPropertyId,
            allowedDepartmentIds,
            cancellationToken);
        if (!access.IsSuccess)
        {
            return access.Error!;
        }

        var request = access.Value;
        if (request.ApprovalStage == LeaveRequestApprovalStage.Department)
        {
            if (!canApprove && !canManage)
            {
                return WorkforceError.LeaveRequestApprovalPermissionDenied();
            }
        }
        else if (request.ApprovalStage == LeaveRequestApprovalStage.Hr)
        {
            if (!canManage)
            {
                return WorkforceError.LeaveRequestApprovalPermissionDenied();
            }
        }
        else
        {
            return WorkforceError.LeaveRequestInvalidApprovalStage();
        }

        var result = await reject.ExecuteAsync(
            new RejectLeaveRequestCommand(leaveRequestId, note, actorUserId),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        return await MutationResultAsync(result.Value!, cancellationToken);
    }

    public async Task<WorkforceResult<LeaveRequestMutationResultDto>> HrApproveAsync(
        Guid leaveRequestId,
        decimal finalAmount,
        string? note,
        string actorUserId,
        Guid? scopedPropertyId,
        IReadOnlySet<Guid>? allowedDepartmentIds,
        bool canManage,
        CancellationToken cancellationToken)
    {
        if (!canManage)
        {
            return WorkforceError.LeaveRequestApprovalPermissionDenied();
        }

        if (!LeaveAmount.IsValidPositive(finalAmount))
        {
            return WorkforceError.LeaveRequestInvalidFinalAmount();
        }

        var access = await query.ResolveAccessibleRequestAsync(
            leaveRequestId,
            scopedPropertyId,
            allowedDepartmentIds,
            cancellationToken);
        if (!access.IsSuccess)
        {
            return access.Error!;
        }

        if (access.Value.IsFinalized)
        {
            return WorkforceError.LeaveRequestAlreadyFinalized();
        }

        if (access.Value.ApprovalStage != LeaveRequestApprovalStage.Hr)
        {
            return WorkforceError.LeaveRequestInvalidApprovalStage();
        }

        var result = await hrApprove.ExecuteAsync(
            new ApproveLeaveRequestHrCommand(leaveRequestId, finalAmount, note, actorUserId),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        return await MutationResultAsync(result.Value!, cancellationToken);
    }

    public async Task<WorkforceResult<LeaveRequestMutationResultDto>> CancelApprovedAsync(
        Guid leaveRequestId,
        string? reason,
        string actorUserId,
        Guid? scopedPropertyId,
        IReadOnlySet<Guid>? allowedDepartmentIds,
        bool canManage,
        CancellationToken cancellationToken)
    {
        if (!canManage)
        {
            return WorkforceError.LeaveRequestApprovalPermissionDenied();
        }

        var access = await query.ResolveAccessibleRequestAsync(
            leaveRequestId,
            scopedPropertyId,
            allowedDepartmentIds,
            cancellationToken);
        if (!access.IsSuccess)
        {
            return access.Error!;
        }

        var result = await cancelApproved.ExecuteAsync(
            new CancelApprovedLeaveRequestCommand(leaveRequestId, reason, actorUserId),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return result.Error!;
        }

        return await MutationResultAsync(result.Value!, cancellationToken);
    }

    private async Task<WorkforceResult<LeaveRequestMutationResultDto>> MutationResultAsync(
        LeaveRequest request,
        CancellationToken cancellationToken)
    {
        var detail = await composer.ComposeDetailAsync(request, cancellationToken);
        if (detail is null)
        {
            return WorkforceError.LeaveRequestNotFound();
        }

        return new LeaveRequestMutationResultDto(detail, detail.Warnings);
    }
}
