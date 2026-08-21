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
        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
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

        if (!currentEmployment.Value!.TryEnd(command.EndDate, out var endError))
        {
            return endError == "Employment is already ended."
                ? WorkforceError.EmploymentEnded()
                : WorkforceError.InvalidEmploymentPeriod();
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
            currentEmployment.Value.Status);
    }
}

public sealed record EndEmploymentCommand(Guid EmployeeId, DateOnly EndDate);

public sealed record EndedEmployment(
    Guid EmployeeId,
    Guid EmploymentId,
    DateOnly StartDate,
    DateOnly EndDate,
    EmploymentStatus Status);
