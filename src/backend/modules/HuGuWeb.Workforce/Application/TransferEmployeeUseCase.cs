using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class TransferEmployeeUseCase(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<TransferredEmployee>> ExecuteAsync(
        TransferEmployeeCommand command,
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

        var department = await store.GetDepartmentAsync(command.DepartmentId, cancellationToken);
        if (department is null || department.PropertyId != workplace.Value.Property.Id)
        {
            return WorkforceError.DepartmentNotFound();
        }

        var position = await store.GetPositionAsync(command.PositionId, cancellationToken);
        if (position is null || position.PropertyId != workplace.Value.Property.Id)
        {
            return WorkforceError.PositionNotFound();
        }

        var employments = await store.ListEmploymentsAsync(employee.Id, cancellationToken);
        var currentEmployment = CurrentEmployment.Find(employments);
        if (!currentEmployment.IsSuccess)
        {
            return currentEmployment.Error!;
        }

        currentEmployment.Value!.RefreshLifecycle(clock.Today);

        var assignments = await store.ListAssignmentsAsync(currentEmployment.Value.Id, cancellationToken);
        var plan = TransferPlanner.Plan(
            currentEmployment.Value,
            assignments,
            department,
            position,
            command.EffectiveDate);
        if (!plan.IsSuccess)
        {
            return plan.Error!;
        }

        if (!plan.Value.CurrentPrimary.TryCloseOn(plan.Value.PreviousEndDate, out var closeError))
        {
            return closeError == "Assignment end date must be on or after the start date."
                ? WorkforceError.InvalidAssignmentPeriod()
                : WorkforceError.InvalidTransferDate();
        }

        var next = Assignment.StartPrimary(
            Guid.CreateVersion7(),
            currentEmployment.Value.Id,
            plan.Value.NewDepartmentId,
            plan.Value.NewPositionId,
            plan.Value.NewStartDate);

        var planned = assignments
            .Where(item => item.Id != plan.Value.CurrentPrimary.Id)
            .Append(plan.Value.CurrentPrimary)
            .Append(next)
            .ToArray();

        if (PrimaryAssignments.HasOverlap(planned))
        {
            return WorkforceError.OverlappingPrimaryAssignment();
        }

        store.AddAssignment(next);
        await store.SaveChangesAsync(cancellationToken);

        return new TransferredEmployee(
            employee.Id,
            currentEmployment.Value.Id,
            plan.Value.CurrentPrimary.Id,
            plan.Value.PreviousEndDate,
            next.Id,
            next.StartDate,
            department.Id,
            position.Id);
    }
}

public sealed record TransferEmployeeCommand(
    Guid EmployeeId,
    Guid DepartmentId,
    Guid PositionId,
    DateOnly EffectiveDate);

public sealed record TransferredEmployee(
    Guid EmployeeId,
    Guid EmploymentId,
    Guid ClosedAssignmentId,
    DateOnly ClosedAssignmentEndDate,
    Guid NewAssignmentId,
    DateOnly NewAssignmentStartDate,
    Guid DepartmentId,
    Guid PositionId);
