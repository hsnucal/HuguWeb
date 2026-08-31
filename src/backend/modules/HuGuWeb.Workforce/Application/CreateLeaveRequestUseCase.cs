using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class CreateLeaveRequestUseCase(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<LeaveRequest>> ExecuteAsync(
        CreateLeaveRequestCommand command,
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
        var organizationId = context.Value.OrganizationId;

        var leaveType = await store.GetLeaveTypeAsync(command.LeaveTypeId, cancellationToken);
        if (leaveType is null || leaveType.OrganizationId != organizationId)
        {
            return WorkforceError.LeaveTypeNotFound();
        }

        if (!leaveType.IsActive)
        {
            return WorkforceError.LeaveRequestTypeInactive();
        }

        if (command.StartDate < employment.StartDate
            || (employment.EndDate is { } end && command.EndDate > end))
        {
            return WorkforceError.LeaveRequestDateOutsideEmployment();
        }

        if (command.StartDate > command.EndDate)
        {
            return WorkforceError.LeaveValidationField(
                LeaveValidation.Fields.EndDate,
                LeaveValidation.Codes.LeaveInvalidDateRange,
                "The leave start date cannot be after the end date.");
        }

        if (!LeaveAmount.IsValidPositive(command.RequestedAmount))
        {
            return WorkforceError.LeaveValidationField(
                LeaveValidation.Fields.RequestedAmount,
                LeaveValidation.Codes.LeaveRequestInvalidAmount,
                "Leave request amount must be greater than zero and a multiple of 0.5 days.");
        }

        var assignments = await store.ListAssignmentsAsync(employment.Id, cancellationToken);
        if (!LeaveRequestAssignment.TryResolveForRange(
                assignments,
                command.StartDate,
                command.EndDate,
                out var assignment,
                out var assignmentError))
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
                command.StartDate,
                command.EndDate))
        {
            return WorkforceError.LeaveRequestOverlap();
        }

        if (!LeaveRequest.TryCreate(
                Guid.CreateVersion7(),
                employment.Id,
                assignment!.Id,
                leaveType.Id,
                command.StartDate,
                command.EndDate,
                command.RequestedAmount,
                command.Reason,
                command.ActorUserId,
                clock.UtcNow,
                out var request,
                out var field,
                out var errorCode))
        {
            return WorkforceError.LeaveValidationField(
                field!,
                errorCode!,
                "The leave request is invalid.");
        }

        store.AddLeaveRequest(request!);
        await store.SaveChangesAsync(cancellationToken);
        return request!;
    }
}

public sealed record CreateLeaveRequestCommand(
    Guid EmployeeId,
    Guid? EmploymentId,
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal RequestedAmount,
    string? Reason,
    string ActorUserId);
