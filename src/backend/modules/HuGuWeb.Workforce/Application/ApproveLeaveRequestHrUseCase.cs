using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

/// <summary>
/// HR final approval: Pending+Hr → Approved+Done, creates exactly one LeaveRecord with
/// SourceLeaveRequestId in a single transaction. Idempotent against double approve via
/// status/stage checks and unique SourceLeaveRequestId.
/// Inactive LeaveType after submit does not block final approval.
/// </summary>
public sealed class ApproveLeaveRequestHrUseCase(
    IWorkforceStore store,
    IWorkforceClock clock)
{
    public async Task<WorkforceResult<LeaveRequest>> ExecuteAsync(
        ApproveLeaveRequestHrCommand command,
        CancellationToken cancellationToken)
    {
        var request = await store.GetLeaveRequestAsync(command.LeaveRequestId, cancellationToken);
        if (request is null)
        {
            return WorkforceError.LeaveRequestNotFound();
        }

        if (!request.IsPending || request.ApprovalStage != LeaveRequestApprovalStage.Hr)
        {
            if (request.IsFinalized)
            {
                return WorkforceError.LeaveRequestAlreadyFinalized();
            }

            if (!request.IsPending)
            {
                return WorkforceError.LeaveRequestNotPending();
            }

            return WorkforceError.LeaveRequestInvalidApprovalStage();
        }

        if (!LeaveAmount.IsValidPositive(command.FinalAmount))
        {
            return WorkforceError.LeaveValidationField(
                LeaveValidation.Fields.FinalAmount,
                LeaveValidation.Codes.LeaveRequestInvalidAmount,
                "Final leave amount must be greater than zero and a multiple of 0.5 days.");
        }

        var employment = await store.GetEmploymentAsync(request.EmploymentId, cancellationToken);
        if (employment is null)
        {
            return WorkforceError.EmploymentNotFound();
        }

        if (request.StartDate < employment.StartDate
            || (employment.EndDate is { } end && request.EndDate > end))
        {
            return WorkforceError.LeaveRequestDateOutsideEmployment();
        }

        var assignments = await store.ListAssignmentsAsync(employment.Id, cancellationToken);
        if (!LeaveRequestAssignment.TryResolveForRange(
                assignments,
                request.StartDate,
                request.EndDate,
                out var assignment,
                out var assignmentError)
            || assignment!.Id != request.AssignmentId)
        {
            return assignmentError switch
            {
                LeaveValidation.Codes.LeaveRequestAssignmentNotFound =>
                    WorkforceError.LeaveRequestAssignmentNotFound(),
                _ => WorkforceError.LeaveRequestCrossAssignmentRange()
            };
        }

        var existingRequests = await store.ListLeaveRequestsAsync(employment.Id, cancellationToken);
        var existingRecords = await store.ListLeaveRecordsAsync(employment.Id, cancellationToken);
        if (LeaveRequestOverlap.BlocksCreateOrApprove(
                existingRequests,
                existingRecords,
                request.StartDate,
                request.EndDate,
                ignoreRequestId: request.Id))
        {
            return WorkforceError.LeaveRequestOverlap();
        }

        var existingLinked = await store.FindLeaveRecordBySourceLeaveRequestIdAsync(
            request.Id,
            cancellationToken);
        if (existingLinked is not null)
        {
            return WorkforceError.LeaveRequestRecordConflict();
        }

        if (!LeaveRecord.TryCreate(
                Guid.CreateVersion7(),
                request.EmploymentId,
                request.LeaveTypeId,
                request.StartDate,
                request.EndDate,
                command.FinalAmount,
                note: request.Reason,
                command.ActorUserId,
                clock.UtcNow,
                out var record,
                out var field,
                out var createError,
                sourceLeaveRequestId: request.Id))
        {
            return WorkforceError.LeaveValidationField(field!, createError!, "The leave record is invalid.");
        }

        await using var transaction = await store.BeginTransactionAsync(cancellationToken);
        try
        {
            if (!request.TryApproveHr(
                    command.ActorUserId,
                    clock.UtcNow,
                    command.Note,
                    out var decision,
                    out var transitionError))
            {
                await transaction.RollbackAsync(cancellationToken);
                return transitionError switch
                {
                    LeaveValidation.Codes.LeaveRequestAlreadyFinalized =>
                        WorkforceError.LeaveRequestAlreadyFinalized(),
                    LeaveValidation.Codes.LeaveRequestInvalidApprovalStage =>
                        WorkforceError.LeaveRequestInvalidApprovalStage(),
                    _ => WorkforceError.LeaveRequestNotPending()
                };
            }

            store.AddLeaveRecord(record!);
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

public sealed record ApproveLeaveRequestHrCommand(
    Guid LeaveRequestId,
    decimal FinalAmount,
    string? Note,
    string ActorUserId);
