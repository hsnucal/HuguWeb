using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class CancelWorkforceMovementUseCase(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<PersonnelMovementDetailDto>> ExecuteAsync(
        CancelPersonnelMovementCommand command,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var movement = await store.GetPersonnelMovementAsync(command.MovementId, cancellationToken);
        if (movement is null || movement.OrganizationId != workplace.Value.Organization.Id)
        {
            return WorkforceError.MovementNotFound();
        }

        if (movement.IsCancelled)
        {
            return WorkforceError.MovementAlreadyCancelled();
        }

        var trimmedReason = command.Reason?.Trim() ?? string.Empty;
        if (trimmedReason.Length == 0)
        {
            return WorkforceError.MovementField(
                MovementValidation.Fields.CancellationReason,
                MovementValidation.Codes.CancellationReasonRequired,
                "A cancellation reason is required.");
        }

        if (trimmedReason.Length > PersonnelMovement.ReasonMaxLength)
        {
            return WorkforceError.MovementField(
                MovementValidation.Fields.CancellationReason,
                MovementValidation.Codes.CancellationReasonTooLong,
                "The cancellation reason is too long.");
        }

        var employment = await store.GetEmploymentAsync(movement.EmploymentId, cancellationToken);
        if (employment is null)
        {
            return WorkforceError.MovementEmploymentNotFound();
        }

        var calendarProperty = await ResolveCalendarPropertyAsync(movement, cancellationToken);
        if (calendarProperty is null)
        {
            return WorkforceError.WorkplaceNotConfigured();
        }

        if (!MovementPropertyAccess.CanAccess(command.AccessiblePropertyIds, calendarProperty.Id)
            && movement.PreviousAssignmentId is { } previousAssignmentId)
        {
            var previous = await store.GetAssignmentAsync(previousAssignmentId, cancellationToken);
            if (previous is not null)
            {
                var previousDepartment = await store.GetDepartmentAsync(previous.DepartmentId, cancellationToken);
                if (previousDepartment is not null
                    && !MovementPropertyAccess.CanAccess(command.AccessiblePropertyIds, previousDepartment.PropertyId))
                {
                    return WorkforceError.MovementPropertyAccessDenied();
                }
            }
        }
        else if (!MovementPropertyAccess.CanAccess(command.AccessiblePropertyIds, calendarProperty.Id))
        {
            return WorkforceError.MovementPropertyAccessDenied();
        }

        var today = PropertyLocalCalendar.Today(clock.UtcNow, calendarProperty.TimeZoneId);
        if (movement.EffectiveDate <= today)
        {
            return WorkforceError.MovementAlreadyEffective();
        }

        if (movement.MovementType == PersonnelMovementType.ManagerChange)
        {
            var rollback = await RollbackReportingLineAsync(movement, cancellationToken);
            if (!rollback.IsSuccess)
            {
                return rollback.Error!;
            }
        }
        else
        {
            var rollback = await RollbackAssignmentAsync(movement, cancellationToken);
            if (!rollback.IsSuccess)
            {
                return rollback.Error!;
            }
        }

        if (!movement.TryCancel(trimmedReason, command.ActorUserId, clock.UtcNow, out var field, out var errorCode))
        {
            return errorCode == MovementValidation.Codes.AlreadyCancelled
                ? WorkforceError.MovementAlreadyCancelled()
                : WorkforceError.MovementField(
                    field ?? MovementValidation.Fields.CancellationReason,
                    errorCode ?? MovementValidation.Codes.CancellationReasonRequired,
                    "A cancellation reason is required.");
        }

        await store.SaveChangesAsync(cancellationToken);
        return await PersonnelMovementComposer.ComposeAsync(
            store,
            movement,
            calendarProperty,
            clock.UtcNow,
            cancellationToken);
    }

    private async Task<WorkforceResult> RollbackAssignmentAsync(
        PersonnelMovement movement,
        CancellationToken cancellationToken)
    {
        if (movement.NewAssignmentId is null || movement.PreviousAssignmentId is null)
        {
            return WorkforceError.MovementNotCancellable();
        }

        var next = await store.GetAssignmentAsync(movement.NewAssignmentId.Value, cancellationToken);
        var previous = await store.GetAssignmentAsync(movement.PreviousAssignmentId.Value, cancellationToken);
        if (next is null || previous is null)
        {
            return WorkforceError.MovementNotCancellable();
        }

        if (next.EndDate is not null)
        {
            return WorkforceError.MovementNotCancellable();
        }

        var later = await store.ListPersonnelMovementsAsync(
            movement.OrganizationId,
            dateFrom: movement.EffectiveDate,
            dateTo: null,
            type: null,
            employmentIds: [movement.EmploymentId],
            cancellationToken);
        if (later.Any(item =>
                !item.IsCancelled
                && item.Id != movement.Id
                && item.PreviousAssignmentId == next.Id))
        {
            return WorkforceError.MovementNotCancellable();
        }

        previous.Reopen();
        movement.DetachNeverEffectiveSuccessor();
        store.RemoveAssignment(next);
        return WorkforceResult.Success();
    }

    private async Task<WorkforceResult> RollbackReportingLineAsync(
        PersonnelMovement movement,
        CancellationToken cancellationToken)
    {
        if (movement.NewReportingLineId is { } newLineId)
        {
            var next = await store.GetReportingLineAsync(newLineId, cancellationToken);
            if (next is null || next.EffectiveTo is not null)
            {
                return WorkforceError.MovementNotCancellable();
            }

            store.RemoveReportingLine(next);
        }

        movement.DetachNeverEffectiveSuccessor();

        if (movement.PreviousReportingLineId is { } previousLineId)
        {
            var previous = await store.GetReportingLineAsync(previousLineId, cancellationToken);
            if (previous is null)
            {
                return WorkforceError.MovementNotCancellable();
            }

            previous.Reopen();
        }

        return WorkforceResult.Success();
    }

    private async Task<Property?> ResolveCalendarPropertyAsync(
        PersonnelMovement movement,
        CancellationToken cancellationToken)
    {
        var assignmentId = movement.NewAssignmentId ?? movement.PreviousAssignmentId;
        if (assignmentId is { } id)
        {
            var assignment = await store.GetAssignmentAsync(id, cancellationToken);
            if (assignment is not null)
            {
                var department = await store.GetDepartmentAsync(assignment.DepartmentId, cancellationToken);
                if (department is not null)
                {
                    return await store.GetPropertyAsync(department.PropertyId, cancellationToken);
                }
            }
        }

        var employment = await store.GetEmploymentAsync(movement.EmploymentId, cancellationToken);
        if (employment is null)
        {
            return null;
        }

        var assignments = await store.ListAssignmentsAsync(employment.Id, cancellationToken);
        var covering = PrimaryAssignments.Covering(assignments, movement.EffectiveDate)
            ?? PrimaryAssignments.OrderedPrimaries(assignments).FirstOrDefault();
        if (covering is null)
        {
            return null;
        }

        var coveringDepartment = await store.GetDepartmentAsync(covering.DepartmentId, cancellationToken);
        return coveringDepartment is null
            ? null
            : await store.GetPropertyAsync(coveringDepartment.PropertyId, cancellationToken);
    }
}

public sealed record CancelPersonnelMovementCommand(
    Guid MovementId,
    string Reason,
    string ActorUserId,
    IReadOnlySet<Guid>? AccessiblePropertyIds);
