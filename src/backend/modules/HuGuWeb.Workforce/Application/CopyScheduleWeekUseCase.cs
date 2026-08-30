using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

/// <summary>
/// HR-06B copy previous week. Source AssignmentId is never reused — each target date re-resolves
/// Employment/Primary Assignment and re-authorizes. Inactive ShiftDefinitions block target copy
/// (copy is a new assignment). Unscheduled source cells are not copied.
/// </summary>
public sealed class CopyScheduleWeekUseCase(
    IWorkforceStore store,
    IWorkplaceContext workplaceContext,
    GetScheduleWeekQuery weekQuery,
    BulkScheduleUseCase bulk)
{
    public Task<WorkforceResult<CopyScheduleWeekPreviewDto>> PreviewAsync(
        CopyScheduleWeekCommand command,
        CancellationToken cancellationToken) =>
        BuildPreviewAsync(command, cancellationToken);

    public async Task<WorkforceResult<BulkScheduleResultDto>> ExecuteAsync(
        CopyScheduleWeekCommand command,
        CancellationToken cancellationToken)
    {
        var preview = await BuildPreviewAsync(command, cancellationToken);
        if (!preview.IsSuccess)
        {
            return preview.Error!;
        }

        if (preview.Value!.Invalid.Count > 0)
        {
            return WorkforceError.ScheduleCopyWeekBlocked(preview.Value);
        }

        if (preview.Value.Operations.Count == 0)
        {
            return WorkforceError.ScheduleValidationField(
                ScheduleValidation.Fields.WeekStart,
                ScheduleValidation.Codes.ScheduleCopyWeekEmpty,
                "No Shift or RestDay entries are available to copy from the previous week.");
        }

        var operations = preview.Value.Operations
            .Select(item => new BulkScheduleOperation(
                item.EmployeeId,
                item.TargetDate,
                Clear: false,
                item.Kind == "RestDay" ? ScheduleEntryKind.RestDay : ScheduleEntryKind.Shift,
                item.ShiftDefinitionId,
                Note: null))
            .ToArray();

        return await bulk.ExecuteAsync(
            new BulkScheduleCommand(
                operations,
                command.ActorUserId,
                command.ScopedPropertyId,
                command.AllowedDepartmentIds),
            cancellationToken);
    }

    private async Task<WorkforceResult<CopyScheduleWeekPreviewDto>> BuildPreviewAsync(
        CopyScheduleWeekCommand command,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        if (command.TargetWeekStart.DayOfWeek != DayOfWeek.Monday)
        {
            return WorkforceError.ScheduleValidationField(
                ScheduleValidation.Fields.WeekStart,
                ScheduleValidation.Codes.ScheduleWeekStartInvalid,
                "Week start must be a Monday (Property-local DateOnly).");
        }

        var sourceWeekStart = command.TargetWeekStart.AddDays(-7);
        var sourceWeekEnd = sourceWeekStart.AddDays(6);
        var targetWeekEnd = command.TargetWeekStart.AddDays(6);

        // Use week query to discover authorized employees for the source week under the same filter.
        var sourceWeek = await weekQuery.ExecuteAsync(
            sourceWeekStart,
            command.DepartmentId,
            command.ScopedPropertyId,
            command.AllowedDepartmentIds,
            cancellationToken);
        if (!sourceWeek.IsSuccess)
        {
            return sourceWeek.Error!;
        }

        var operations = new List<CopyScheduleWeekOperationDto>();
        var invalid = new List<CopyScheduleWeekInvalidDto>();
        var overwriteCount = 0;

        foreach (var employee in sourceWeek.Value!.Employees)
        {
            foreach (var cell in employee.Cells)
            {
                if (cell.Eligibility != ScheduleWeekCellDto.EligibilityEditable)
                {
                    continue;
                }

                if (cell.State is not ("Shift" or "RestDay"))
                {
                    continue;
                }

                var dayOffset = cell.Date.DayNumber - sourceWeekStart.DayNumber;
                var targetDate = command.TargetWeekStart.AddDays(dayOffset);

                var validation = await ValidateTargetAsync(
                    employee.EmployeeId,
                    targetDate,
                    cell,
                    command,
                    cancellationToken);

                if (!validation.IsSuccess)
                {
                    invalid.Add(new CopyScheduleWeekInvalidDto(
                        employee.EmployeeId,
                        employee.GivenName,
                        employee.FamilyName,
                        employee.PersonnelNumber,
                        cell.Date,
                        targetDate,
                        validation.Error!.Code,
                        validation.Error.Detail));
                    continue;
                }

                if (validation.Value!.WouldOverwrite)
                {
                    overwriteCount++;
                }

                operations.Add(new CopyScheduleWeekOperationDto(
                    employee.EmployeeId,
                    employee.GivenName,
                    employee.FamilyName,
                    employee.PersonnelNumber,
                    cell.Date,
                    targetDate,
                    cell.State!,
                    cell.ShiftDefinitionId,
                    cell.ShiftCode,
                    cell.ShiftName,
                    validation.Value.WouldOverwrite,
                    validation.Value.TargetAssignmentId,
                    validation.Value.TargetDepartmentId,
                    validation.Value.TargetDepartmentName));
            }
        }

        return new CopyScheduleWeekPreviewDto(
            sourceWeekStart,
            sourceWeekEnd,
            command.TargetWeekStart,
            targetWeekEnd,
            command.DepartmentId,
            operations.Count,
            overwriteCount,
            invalid.Count,
            operations,
            invalid);
    }

    private async Task<WorkforceResult<CopyTargetValidation>> ValidateTargetAsync(
        Guid employeeId,
        DateOnly targetDate,
        ScheduleWeekCellDto sourceCell,
        CopyScheduleWeekCommand command,
        CancellationToken cancellationToken)
    {
        var employee = await store.GetEmployeeAsync(employeeId, cancellationToken);
        if (employee is null || employee.OrganizationId != workplaceContext.OrganizationId)
        {
            return WorkforceError.EmployeeNotFound();
        }

        var employments = await store.ListEmploymentsAsync(employeeId, cancellationToken);
        var employmentResult = ScheduleEmploymentResolver.ResolveCovering(employments, targetDate);
        if (!employmentResult.IsSuccess)
        {
            return employmentResult.Error!;
        }

        var workplace = await ScheduleWorkplaceResolver.ResolveAsync(
            store,
            employmentResult.Value!,
            targetDate,
            cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        if (!ScheduleAccess.AllowsWorkplace(
                command.ScopedPropertyId,
                command.AllowedDepartmentIds,
                workplace.Value!.Property.Id,
                workplace.Value.Department.Id))
        {
            return WorkforceError.SchedulePropertyAccessDenied();
        }

        if (command.DepartmentId is { } filterDept
            && workplace.Value.Department.Id != filterDept)
        {
            return WorkforceError.ScheduleValidationField(
                ScheduleValidation.Fields.DepartmentId,
                ScheduleValidation.Codes.ScheduleDepartmentFilterDenied,
                "Target date workplace is outside the selected department filter.");
        }

        if (sourceCell.State == "Shift")
        {
            if (sourceCell.ShiftDefinitionId is not { } shiftDefinitionId)
            {
                return WorkforceError.ScheduleValidationField(
                    ScheduleValidation.Fields.ShiftDefinitionId,
                    ScheduleValidation.Codes.ScheduleShiftDefinitionRequired,
                    "A shift definition is required for Kind=Shift.");
            }

            var definition = await store.GetShiftDefinitionAsync(shiftDefinitionId, cancellationToken);
            if (definition is null)
            {
                return WorkforceError.ShiftDefinitionNotFound();
            }

            // Copy is a NEW assignment — inactive definitions are rejected.
            if (!definition.IsActive)
            {
                return WorkforceError.ShiftDefinitionInactive();
            }

            if (definition.PropertyId != workplace.Value.Property.Id)
            {
                return WorkforceError.ScheduleCrossPropertyShift();
            }
        }

        var existing = await store.GetScheduleEntryAsync(
            workplace.Value.Employment.Id,
            targetDate,
            cancellationToken);

        return new CopyTargetValidation(
            WouldOverwrite: existing is not null,
            workplace.Value.Assignment.Id,
            workplace.Value.Department.Id,
            workplace.Value.Department.Name);
    }

    private sealed record CopyTargetValidation(
        bool WouldOverwrite,
        Guid TargetAssignmentId,
        Guid TargetDepartmentId,
        string TargetDepartmentName);
}

public sealed record CopyScheduleWeekCommand(
    DateOnly TargetWeekStart,
    Guid? DepartmentId,
    string ActorUserId,
    Guid? ScopedPropertyId,
    IReadOnlySet<Guid>? AllowedDepartmentIds = null);

public sealed record CopyScheduleWeekPreviewDto(
    DateOnly SourceWeekStart,
    DateOnly SourceWeekEnd,
    DateOnly TargetWeekStart,
    DateOnly TargetWeekEnd,
    Guid? DepartmentId,
    int CopyCount,
    int OverwriteCount,
    int InvalidCount,
    IReadOnlyList<CopyScheduleWeekOperationDto> Operations,
    IReadOnlyList<CopyScheduleWeekInvalidDto> Invalid);

public sealed record CopyScheduleWeekOperationDto(
    Guid EmployeeId,
    string GivenName,
    string FamilyName,
    string PersonnelNumber,
    DateOnly SourceDate,
    DateOnly TargetDate,
    string Kind,
    Guid? ShiftDefinitionId,
    string? ShiftCode,
    string? ShiftName,
    bool WouldOverwrite,
    Guid TargetAssignmentId,
    Guid TargetDepartmentId,
    string TargetDepartmentName);

public sealed record CopyScheduleWeekInvalidDto(
    Guid EmployeeId,
    string GivenName,
    string FamilyName,
    string PersonnelNumber,
    DateOnly SourceDate,
    DateOnly TargetDate,
    string Code,
    string Detail);
