using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class CreateWorkforceMovementUseCase(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<PersonnelMovementDetailDto>> ExecuteAsync(
        CreatePersonnelMovementCommand command,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var organization = workplace.Value.Organization;
        var employment = await ResolveEmploymentAsync(command, organization.Id, cancellationToken);
        if (!employment.IsSuccess)
        {
            return employment.Error!;
        }

        employment.Value!.RefreshLifecycle(clock.Today);
        if (employment.Value.IsEnded)
        {
            return WorkforceError.EmploymentEnded();
        }

        if (command.Type is PersonnelMovementType.AssignmentChange && !command.AllowLegacyAssignmentChange)
        {
            return WorkforceError.MovementInvalidType();
        }

        return command.Type switch
        {
            PersonnelMovementType.ManagerChange =>
                await ExecuteManagerChangeAsync(command, organization, employment.Value, cancellationToken),
            PersonnelMovementType.DepartmentChange
                or PersonnelMovementType.PositionChange
                or PersonnelMovementType.Promotion
                or PersonnelMovementType.PropertyTransfer
                or PersonnelMovementType.AssignmentChange =>
                await ExecuteAssignmentMovementAsync(command, organization, employment.Value, cancellationToken),
            _ => WorkforceError.MovementInvalidType()
        };
    }

    private async Task<WorkforceResult<Employment>> ResolveEmploymentAsync(
        CreatePersonnelMovementCommand command,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        if (command.EmploymentId is { } employmentId)
        {
            var employment = await store.GetEmploymentAsync(employmentId, cancellationToken);
            if (employment is null)
            {
                return WorkforceError.MovementEmploymentNotFound();
            }

            var employee = await store.GetEmployeeAsync(employment.EmployeeId, cancellationToken);
            if (employee is null || employee.OrganizationId != organizationId)
            {
                return WorkforceError.MovementEmploymentNotFound();
            }

            return employment;
        }

        if (command.EmployeeId is { } employeeId)
        {
            var employee = await store.GetEmployeeAsync(employeeId, cancellationToken);
            if (employee is null || employee.OrganizationId != organizationId)
            {
                return WorkforceError.EmployeeNotFound();
            }

            var employments = await store.ListEmploymentsAsync(employee.Id, cancellationToken);
            var current = CurrentEmployment.Find(employments);
            return current.IsSuccess ? current.Value! : current.Error!;
        }

        return WorkforceError.MovementEmploymentNotFound();
    }

    private async Task<WorkforceResult<PersonnelMovementDetailDto>> ExecuteAssignmentMovementAsync(
        CreatePersonnelMovementCommand command,
        Organization organization,
        Employment employment,
        CancellationToken cancellationToken)
    {
        var assignments = await store.ListAssignmentsAsync(employment.Id, cancellationToken);
        var overlappingProbe = PrimaryAssignments.OrderedPrimaries(assignments)
            .Where(item => item.Period.Overlaps(new DatePeriod(command.EffectiveDate, null)))
            .ToArray();
        if (overlappingProbe.Length == 0)
        {
            return WorkforceError.InvalidTransferDate();
        }

        if (overlappingProbe.Length > 1)
        {
            return WorkforceError.OverlappingPrimaryAssignment();
        }

        var current = overlappingProbe[0];
        var currentDepartment = await store.GetDepartmentAsync(current.DepartmentId, cancellationToken);
        if (currentDepartment is null)
        {
            return WorkforceError.DepartmentNotFound();
        }

        var sourceProperty = await store.GetPropertyAsync(currentDepartment.PropertyId, cancellationToken);
        if (sourceProperty is null)
        {
            return WorkforceError.WorkplaceNotConfigured();
        }

        if (!MovementPropertyAccess.CanAccess(command.AccessiblePropertyIds, sourceProperty.Id))
        {
            return WorkforceError.MovementPropertyAccessDenied();
        }

        var dest = await ResolveDestinationAsync(
            command,
            current,
            currentDepartment,
            sourceProperty,
            organization.Id,
            cancellationToken);
        if (!dest.IsSuccess)
        {
            return dest.Error!;
        }

        var destDepartment = dest.Value.Department;
        var destPosition = dest.Value.Position;
        var destProperty = dest.Value.Property;

        if (command.Type == PersonnelMovementType.PropertyTransfer)
        {
            if (!MovementPropertyAccess.CanAccess(command.AccessiblePropertyIds, destProperty.Id))
            {
                return WorkforceError.MovementPropertyAccessDenied();
            }
        }
        else if (destProperty.Id != sourceProperty.Id)
        {
            return WorkforceError.MovementInvalidType();
        }

        var leaveConflict = await HasPendingLeaveSplitAsync(employment.Id, command.EffectiveDate, cancellationToken);
        if (leaveConflict)
        {
            return WorkforceError.MovementPendingLeaveConflict();
        }

        var scheduleConflict = await HasFutureStructuralConflictAsync(
            employment.Id,
            current.Id,
            command.EffectiveDate,
            cancellationToken);
        if (scheduleConflict)
        {
            return WorkforceError.MovementScheduleConflict();
        }

        var applicable = await store.IsPositionApplicableToDepartmentAsync(
            destDepartment.Id,
            destPosition.Id,
            cancellationToken);
        var plan = TransferPlanner.Plan(
            employment,
            assignments,
            destDepartment,
            destPosition,
            applicable,
            command.EffectiveDate);
        if (!plan.IsSuccess)
        {
            return command.UseLegacyErrorCodes
                ? plan.Error!
                : MapPlannerError(plan.Error!, command.Type);
        }

        if (!plan.Value.CurrentPrimary.TryCloseOn(plan.Value.PreviousEndDate, out var closeError))
        {
            return closeError == "Assignment end date must be on or after the start date."
                ? WorkforceError.InvalidAssignmentPeriod()
                : WorkforceError.InvalidTransferDate();
        }

        var next = Assignment.StartPrimary(
            Guid.CreateVersion7(),
            employment.Id,
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
            plan.Value.CurrentPrimary.Reopen();
            return WorkforceError.OverlappingPrimaryAssignment();
        }

        if (!PersonnelMovement.TryCreate(
                Guid.CreateVersion7(),
                organization.Id,
                employment.Id,
                command.Type,
                command.EffectiveDate,
                plan.Value.CurrentPrimary.Id,
                next.Id,
                previousReportingLineId: null,
                newReportingLineId: null,
                command.Reason,
                command.Note,
                command.ActorUserId,
                clock.UtcNow,
                out var movement,
                out var field,
                out var errorCode))
        {
            plan.Value.CurrentPrimary.Reopen();
            return WorkforceError.MovementField(
                field ?? MovementValidation.Fields.Reason,
                errorCode ?? MovementValidation.Codes.ReasonRequired,
                "The movement reason is invalid.");
        }

        store.AddAssignment(next);
        store.AddPersonnelMovement(movement!);
        await store.SaveChangesAsync(cancellationToken);

        return await PersonnelMovementComposer.ComposeAsync(
            store,
            movement!,
            destProperty,
            clock.UtcNow,
            cancellationToken);
    }

    private static WorkforceError MapPlannerError(WorkforceError error, PersonnelMovementType type)
    {
        if (error.Code == "same-assignment")
        {
            return WorkforceError.MovementSameTarget();
        }

        if (error.Code == "position-not-available-for-department"
            && type is PersonnelMovementType.DepartmentChange
                or PersonnelMovementType.PositionChange
                or PersonnelMovementType.Promotion
                or PersonnelMovementType.PropertyTransfer)
        {
            return WorkforceError.MovementPositionNotApplicable();
        }

        return error;
    }

    private async Task<WorkforceResult<AssignmentDestinationPair>> ResolveDestinationAsync(
        CreatePersonnelMovementCommand command,
        Assignment current,
        Department currentDepartment,
        Property sourceProperty,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        switch (command.Type)
        {
            case PersonnelMovementType.DepartmentChange:
            {
                if (command.TargetDepartmentId is null)
                {
                    return WorkforceError.MovementField(
                        MovementValidation.Fields.TargetDepartmentId,
                        MovementValidation.Codes.TargetDepartmentRequired,
                        "A target department is required.");
                }

                var department = await store.GetDepartmentAsync(command.TargetDepartmentId.Value, cancellationToken);
                if (department is null)
                {
                    return WorkforceError.DepartmentNotFound();
                }

                var destProperty = await store.GetPropertyAsync(department.PropertyId, cancellationToken);
                if (destProperty is null || destProperty.OrganizationId != organizationId)
                {
                    return WorkforceError.MovementCrossOrganizationNotSupported();
                }

                if (department.PropertyId != sourceProperty.Id)
                {
                    return WorkforceError.MovementInvalidType();
                }

                Guid positionId;
                if (command.TargetPositionId is { } requestedPosition)
                {
                    positionId = requestedPosition;
                }
                else
                {
                    var keepApplicable = await store.IsPositionApplicableToDepartmentAsync(
                        department.Id,
                        current.PositionId,
                        cancellationToken);
                    if (!keepApplicable)
                    {
                        return WorkforceError.MovementField(
                            MovementValidation.Fields.TargetPositionId,
                            MovementValidation.Codes.TargetPositionRequired,
                            "The current position is not applicable to the target department. Supply a target position.");
                    }

                    positionId = current.PositionId;
                }

                var position = await store.GetPositionAsync(positionId, cancellationToken);
                if (position is null || position.PropertyId != destProperty.Id)
                {
                    return WorkforceError.PositionNotFound();
                }

                return new AssignmentDestinationPair(department, position, destProperty);
            }
            case PersonnelMovementType.PositionChange:
            case PersonnelMovementType.Promotion:
            {
                if (command.TargetPositionId is null)
                {
                    return WorkforceError.MovementField(
                        MovementValidation.Fields.TargetPositionId,
                        MovementValidation.Codes.TargetPositionRequired,
                        "A target position is required.");
                }

                if (command.TargetDepartmentId is { } explicitDepartment
                    && explicitDepartment != current.DepartmentId)
                {
                    return WorkforceError.MovementInvalidType();
                }

                var position = await store.GetPositionAsync(command.TargetPositionId.Value, cancellationToken);
                if (position is null)
                {
                    return WorkforceError.PositionNotFound();
                }

                if (position.PropertyId != sourceProperty.Id)
                {
                    return WorkforceError.MovementInvalidType();
                }

                if (command.Type == PersonnelMovementType.Promotion
                    && position.Id == current.PositionId)
                {
                    return WorkforceError.MovementSameTarget();
                }

                return new AssignmentDestinationPair(currentDepartment, position, sourceProperty);
            }
            case PersonnelMovementType.PropertyTransfer:
            {
                if (command.TargetPropertyId is null)
                {
                    return WorkforceError.MovementField(
                        MovementValidation.Fields.TargetPropertyId,
                        MovementValidation.Codes.TargetPropertyRequired,
                        "A target property is required.");
                }

                if (command.TargetDepartmentId is null)
                {
                    return WorkforceError.MovementField(
                        MovementValidation.Fields.TargetDepartmentId,
                        MovementValidation.Codes.TargetDepartmentRequired,
                        "A target department is required.");
                }

                if (command.TargetPositionId is null)
                {
                    return WorkforceError.MovementField(
                        MovementValidation.Fields.TargetPositionId,
                        MovementValidation.Codes.TargetPositionRequired,
                        "A target position is required.");
                }

                var destProperty = await store.GetPropertyAsync(command.TargetPropertyId.Value, cancellationToken);
                if (destProperty is null)
                {
                    return WorkforceError.NotFound("property-not-found", "The property was not found.");
                }

                if (destProperty.OrganizationId != organizationId)
                {
                    return WorkforceError.MovementCrossOrganizationNotSupported();
                }

                if (destProperty.Id == sourceProperty.Id)
                {
                    return WorkforceError.MovementSameTarget();
                }

                var department = await store.GetDepartmentAsync(command.TargetDepartmentId.Value, cancellationToken);
                if (department is null || department.PropertyId != destProperty.Id)
                {
                    return WorkforceError.DepartmentNotFound();
                }

                var position = await store.GetPositionAsync(command.TargetPositionId.Value, cancellationToken);
                if (position is null || position.PropertyId != destProperty.Id)
                {
                    return WorkforceError.PositionNotFound();
                }

                return new AssignmentDestinationPair(department, position, destProperty);
            }
            case PersonnelMovementType.AssignmentChange:
            {
                if (command.TargetDepartmentId is null || command.TargetPositionId is null)
                {
                    return WorkforceError.MovementInvalidType();
                }

                var department = await store.GetDepartmentAsync(command.TargetDepartmentId.Value, cancellationToken);
                if (department is null)
                {
                    return WorkforceError.DepartmentNotFound();
                }

                var destProperty = await store.GetPropertyAsync(department.PropertyId, cancellationToken);
                if (destProperty is null || destProperty.OrganizationId != organizationId)
                {
                    return WorkforceError.MovementCrossOrganizationNotSupported();
                }

                var position = await store.GetPositionAsync(command.TargetPositionId.Value, cancellationToken);
                if (position is null || position.PropertyId != destProperty.Id)
                {
                    return WorkforceError.PositionNotFound();
                }

                return new AssignmentDestinationPair(department, position, destProperty);
            }
            default:
                return WorkforceError.MovementInvalidType();
        }
    }

    private async Task<WorkforceResult<PersonnelMovementDetailDto>> ExecuteManagerChangeAsync(
        CreatePersonnelMovementCommand command,
        Organization organization,
        Employment employment,
        CancellationToken cancellationToken)
    {
        var assignments = await store.ListAssignmentsAsync(employment.Id, cancellationToken);
        var covering = PrimaryAssignments.Covering(assignments, command.EffectiveDate)
            ?? PrimaryAssignments.Covering(assignments, command.EffectiveDate.AddDays(-1));
        if (covering is null)
        {
            return WorkforceError.InvalidRequest(
                MovementValidation.Codes.AssignmentNotFound,
                "No primary assignment covers the manager-change effective date.");
        }

        var department = await store.GetDepartmentAsync(covering.DepartmentId, cancellationToken);
        if (department is null)
        {
            return WorkforceError.DepartmentNotFound();
        }

        if (!MovementPropertyAccess.CanAccess(command.AccessiblePropertyIds, department.PropertyId))
        {
            return WorkforceError.MovementPropertyAccessDenied();
        }

        var lines = (await store.ListReportingLinesForEmploymentAsync(employment.Id, cancellationToken)).ToList();
        var newPeriod = new DatePeriod(command.EffectiveDate, null);
        var overlapping = lines.Where(line => line.Period.Overlaps(newPeriod)).ToArray();
        if (overlapping.Length > 1)
        {
            return WorkforceError.ReportingLineOverlap();
        }

        var currentLine = overlapping.Length == 1 ? overlapping[0] : null;
        Guid? targetManagerId = command.ClearManager ? null : command.TargetManagerEmploymentId;

        if (targetManagerId is null && currentLine is null)
        {
            return WorkforceError.MovementSameTarget();
        }

        if (targetManagerId is { } managerEmploymentId)
        {
            if (managerEmploymentId == employment.Id)
            {
                return WorkforceError.ReportingLineSelfManager();
            }

            if (currentLine is not null && currentLine.ManagerEmploymentId == managerEmploymentId)
            {
                return WorkforceError.MovementSameTarget();
            }

            var managerEmployment = await store.GetEmploymentAsync(managerEmploymentId, cancellationToken);
            if (managerEmployment is null)
            {
                return WorkforceError.ReportingLineManagerNotFound();
            }

            var managerEmployee = await store.GetEmployeeAsync(managerEmployment.EmployeeId, cancellationToken);
            if (managerEmployee is null || managerEmployee.OrganizationId != organization.Id)
            {
                return WorkforceError.ReportingLineOrganizationMismatch();
            }

            managerEmployment.RefreshLifecycle(clock.Today);
            if (!managerEmployment.Period.Contains(command.EffectiveDate) || managerEmployment.IsEnded)
            {
                return WorkforceError.ReportingLineManagerNotFound();
            }

            if (await WouldCreateCycleAsync(
                    employment.Id,
                    managerEmploymentId,
                    command.EffectiveDate,
                    cancellationToken))
            {
                return WorkforceError.ReportingLineCycle();
            }
        }

        var previousEnd = command.EffectiveDate.AddDays(-1);
        if (currentLine is not null)
        {
            if (previousEnd < currentLine.EffectiveFrom)
            {
                return WorkforceError.MovementField(
                    MovementValidation.Fields.EffectiveDate,
                    MovementValidation.Codes.EffectiveDateInvalid,
                    "The manager change date would invert the current reporting-line period.");
            }

            if (!currentLine.TryCloseOn(previousEnd, out _))
            {
                return WorkforceError.ReportingLineOverlap();
            }
        }

        WorkforceReportingLine? nextLine = null;
        if (targetManagerId is { } nextManagerId)
        {
            nextLine = WorkforceReportingLine.Start(
                Guid.CreateVersion7(),
                organization.Id,
                employment.Id,
                nextManagerId,
                command.EffectiveDate);

            var planned = new List<WorkforceReportingLine>();
            foreach (var line in lines)
            {
                if (currentLine is null || line.Id != currentLine.Id)
                {
                    planned.Add(line);
                }
            }

            if (currentLine is not null)
            {
                planned.Add(currentLine);
            }

            planned.Add(nextLine);
            if (ReportingLines.HasOverlap(planned))
            {
                currentLine?.Reopen();
                return WorkforceError.ReportingLineOverlap();
            }

            store.AddReportingLine(nextLine);
        }

        if (!PersonnelMovement.TryCreate(
                Guid.CreateVersion7(),
                organization.Id,
                employment.Id,
                PersonnelMovementType.ManagerChange,
                command.EffectiveDate,
                previousAssignmentId: null,
                newAssignmentId: null,
                currentLine?.Id,
                nextLine?.Id,
                command.Reason,
                command.Note,
                command.ActorUserId,
                clock.UtcNow,
                out var movement,
                out var field,
                out var errorCode))
        {
            currentLine?.Reopen();
            return WorkforceError.MovementField(
                field ?? MovementValidation.Fields.Reason,
                errorCode ?? MovementValidation.Codes.ReasonRequired,
                "The movement reason is invalid.");
        }

        store.AddPersonnelMovement(movement!);
        await store.SaveChangesAsync(cancellationToken);

        var calendarProperty = await store.GetPropertyAsync(department.PropertyId, cancellationToken);
        return await PersonnelMovementComposer.ComposeAsync(
            store,
            movement!,
            calendarProperty,
            clock.UtcNow,
            cancellationToken);
    }

    private async Task<bool> HasPendingLeaveSplitAsync(
        Guid employmentId,
        DateOnly effectiveDate,
        CancellationToken cancellationToken)
    {
        var requests = await store.ListLeaveRequestsAsync(employmentId, cancellationToken);
        return requests.Any(item =>
            item.IsPending
            && item.StartDate < effectiveDate
            && item.EndDate >= effectiveDate);
    }

    private async Task<bool> HasFutureStructuralConflictAsync(
        Guid employmentId,
        Guid currentAssignmentId,
        DateOnly effectiveDate,
        CancellationToken cancellationToken)
    {
        var schedule = await store.ListScheduleEntriesAsync(
            [employmentId],
            effectiveDate,
            DateOnly.MaxValue,
            cancellationToken);
        if (schedule.Any(item => item.AssignmentId == currentAssignmentId && item.ScheduleDate >= effectiveDate))
        {
            return true;
        }

        var corrections = await store.ListAttendanceCorrectionsAsync(
            [employmentId],
            effectiveDate,
            DateOnly.MaxValue,
            cancellationToken);
        return corrections.Any(item => item.AssignmentId == currentAssignmentId && item.LocalDate >= effectiveDate);
    }

    private async Task<bool> WouldCreateCycleAsync(
        Guid subordinateEmploymentId,
        Guid managerEmploymentId,
        DateOnly asOf,
        CancellationToken cancellationToken)
    {
        var seen = new HashSet<Guid> { subordinateEmploymentId };
        var current = managerEmploymentId;
        while (current != Guid.Empty)
        {
            if (!seen.Add(current))
            {
                return true;
            }

            var line = await ReportingLineResolver.ForEmploymentOnAsync(
                store,
                current,
                asOf,
                cancellationToken);
            if (line is null)
            {
                break;
            }

            current = line.ManagerEmploymentId;
        }

        return false;
    }

    private sealed record AssignmentDestinationPair(Department Department, Position Position, Property Property);
}

public sealed record CreatePersonnelMovementCommand(
    Guid? EmployeeId,
    Guid? EmploymentId,
    PersonnelMovementType Type,
    DateOnly EffectiveDate,
    Guid? TargetPropertyId,
    Guid? TargetDepartmentId,
    Guid? TargetPositionId,
    Guid? TargetManagerEmploymentId,
    bool ClearManager,
    string Reason,
    string? Note,
    string ActorUserId,
    IReadOnlySet<Guid>? AccessiblePropertyIds,
    bool AllowLegacyAssignmentChange = false,
    bool UseLegacyErrorCodes = false);
