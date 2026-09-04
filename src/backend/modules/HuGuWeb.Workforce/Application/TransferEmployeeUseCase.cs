using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class TransferEmployeeUseCase(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    private readonly CreateWorkforceMovementUseCase _create = new(store, clock, workplaceContext);

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
        var overlapping = PrimaryAssignments.OrderedPrimaries(assignments)
            .Where(item => item.Period.Overlaps(new DatePeriod(command.EffectiveDate, null)))
            .ToArray();
        var current = overlapping.Length == 1 ? overlapping[0] : null;
        var type = PersonnelMovementType.AssignmentChange;
        var allowAssignmentChange = true;
        if (current is not null)
        {
            var currentDepartment = await store.GetDepartmentAsync(current.DepartmentId, cancellationToken);
            var sourcePropertyId = currentDepartment?.PropertyId;
            var destPropertyId = department.PropertyId;
            var deptChanged = current.DepartmentId != department.Id;
            var posChanged = current.PositionId != position.Id;
            if (sourcePropertyId is { } source && source != destPropertyId)
            {
                type = PersonnelMovementType.PropertyTransfer;
                allowAssignmentChange = false;
            }
            else if (deptChanged && posChanged)
            {
                type = PersonnelMovementType.AssignmentChange;
            }
            else if (deptChanged)
            {
                type = PersonnelMovementType.DepartmentChange;
                allowAssignmentChange = false;
            }
            else
            {
                type = PersonnelMovementType.PositionChange;
                allowAssignmentChange = false;
            }
        }

        var created = await _create.ExecuteAsync(
            new CreatePersonnelMovementCommand(
                command.EmployeeId,
                EmploymentId: null,
                type,
                command.EffectiveDate,
                department.PropertyId,
                department.Id,
                position.Id,
                TargetManagerEmploymentId: null,
                ClearManager: false,
                command.Reason ?? "Assignment change",
                Note: null,
                command.ActorUserId ?? "system",
                command.AccessiblePropertyIds,
                allowAssignmentChange,
                UseLegacyErrorCodes: true),
            cancellationToken);
        if (!created.IsSuccess)
        {
            return created.Error!;
        }

        var detail = created.Value!;
        return new TransferredEmployee(
            employee.Id,
            detail.EmploymentId,
            detail.PreviousAssignment!.Id,
            detail.PreviousAssignment.EndDate!.Value,
            detail.NewAssignment!.Id,
            detail.NewAssignment.StartDate,
            department.Id,
            position.Id);
    }
}

public sealed record TransferEmployeeCommand(
    Guid EmployeeId,
    Guid DepartmentId,
    Guid PositionId,
    DateOnly EffectiveDate,
    string? Reason = null,
    string? ActorUserId = null,
    IReadOnlySet<Guid>? AccessiblePropertyIds = null);

public sealed record TransferredEmployee(
    Guid EmployeeId,
    Guid EmploymentId,
    Guid ClosedAssignmentId,
    DateOnly ClosedAssignmentEndDate,
    Guid NewAssignmentId,
    DateOnly NewAssignmentStartDate,
    Guid DepartmentId,
    Guid PositionId);
