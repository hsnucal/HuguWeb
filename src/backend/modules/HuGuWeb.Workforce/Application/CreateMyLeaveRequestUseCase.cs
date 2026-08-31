using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class CreateMyLeaveRequestUseCase(
    IWorkforceStore store,
    CreateLeaveRequestUseCase create,
    LeaveRequestComposer composer)
{
    public async Task<WorkforceResult<LeaveRequestDetailDto>> ExecuteAsync(
        CreateMyLeaveRequestCommand command,
        CancellationToken cancellationToken)
    {
        if (command.LinkedEmployeeId is null)
        {
            return WorkforceError.LeaveRequestAccountLinkRequired();
        }

        var employee = await store.GetEmployeeAsync(command.LinkedEmployeeId.Value, cancellationToken);
        if (employee is null)
        {
            return WorkforceError.LeaveRequestAccountLinkRequired();
        }

        var employments = await store.ListEmploymentsAsync(employee.Id, cancellationToken);
        var current = CurrentEmployment.Find(employments);
        if (!current.IsSuccess)
        {
            return WorkforceError.LeaveRequestCurrentEmploymentNotFound();
        }

        var created = await create.ExecuteAsync(
            new CreateLeaveRequestCommand(
                employee.Id,
                current.Value!.Id,
                command.LeaveTypeId,
                command.StartDate,
                command.EndDate,
                command.RequestedAmount,
                command.Reason,
                command.ActorUserId),
            cancellationToken);
        if (!created.IsSuccess)
        {
            return created.Error!;
        }

        var detail = await composer.ComposeDetailAsync(created.Value!, cancellationToken);
        return detail is null ? WorkforceError.LeaveRequestNotFound() : detail;
    }
}

public sealed record CreateMyLeaveRequestCommand(
    Guid? LinkedEmployeeId,
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal RequestedAmount,
    string? Reason,
    string ActorUserId);
