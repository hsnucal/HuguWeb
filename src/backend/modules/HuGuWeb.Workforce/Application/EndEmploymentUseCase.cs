using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class EndEmploymentUseCase(
    IWorkforceStore store,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<EndedEmployment>> ExecuteAsync(
        EndEmploymentCommand command,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetOrganizationAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var employee = await store.GetEmployeeAsync(command.EmployeeId, cancellationToken);
        if (employee is null || employee.OrganizationId != workplace.Value.Organization.Id)
        {
            return WorkforceError.EmployeeNotFound();
        }

        var employments = await store.ListEmploymentsAsync(employee.Id, cancellationToken);
        var currentEmployment = CurrentEmployment.Find(employments);
        if (!currentEmployment.IsSuccess)
        {
            return currentEmployment.Error!;
        }

        if (command.TerminationReason == default)
        {
            return WorkforceError.TerminationReasonRequired();
        }

        if (!Enum.IsDefined(command.TerminationReason))
        {
            return WorkforceError.InvalidTerminationReason();
        }

        if (!currentEmployment.Value!.TryEnd(command.EndDate, command.TerminationReason, out var endError))
        {
            return endError switch
            {
                "Employment is already ended." => WorkforceError.EmploymentEnded(),
                "Termination reason is invalid." => WorkforceError.InvalidTerminationReason(),
                _ => WorkforceError.InvalidEmploymentPeriod()
            };
        }

        var assignments = await store.ListAssignmentsAsync(currentEmployment.Value.Id, cancellationToken);
        var closed = TransferPlanner.CloseForEmploymentEnd(currentEmployment.Value, assignments, command.EndDate);
        if (!closed.IsSuccess)
        {
            return closed.Error!;
        }

        await store.SaveChangesAsync(cancellationToken);

        return new EndedEmployment(
            employee.Id,
            currentEmployment.Value.Id,
            currentEmployment.Value.StartDate,
            currentEmployment.Value.EndDate!.Value,
            currentEmployment.Value.Status,
            currentEmployment.Value.TerminationReason);
    }
}

public sealed record EndEmploymentCommand(
    Guid EmployeeId,
    DateOnly EndDate,
    EmploymentTerminationReason TerminationReason);

public sealed record EndedEmployment(
    Guid EmployeeId,
    Guid EmploymentId,
    DateOnly StartDate,
    DateOnly EndDate,
    EmploymentStatus Status,
    EmploymentTerminationReason? TerminationReason);
