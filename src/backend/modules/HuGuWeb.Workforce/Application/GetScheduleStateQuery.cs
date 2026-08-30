using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class GetScheduleStateQuery(IWorkforceStore store, IWorkplaceContext workplaceContext)
{
    public Task<WorkforceResult<ScheduleStateDto>> ExecuteAsync(
        Guid employeeId,
        DateOnly scheduleDate,
        Guid? scopedPropertyId,
        CancellationToken cancellationToken) =>
        ExecuteAsync(employeeId, scheduleDate, scopedPropertyId, allowedDepartmentIds: null, cancellationToken);

    public async Task<WorkforceResult<ScheduleStateDto>> ExecuteAsync(
        Guid employeeId,
        DateOnly scheduleDate,
        Guid? scopedPropertyId,
        IReadOnlySet<Guid>? allowedDepartmentIds,
        CancellationToken cancellationToken)
    {
        if (!workplaceContext.HasOrganization)
        {
            return WorkforceError.WorkplaceNotConfigured();
        }

        var employee = await store.GetEmployeeAsync(employeeId, cancellationToken);
        if (employee is null || employee.OrganizationId != workplaceContext.OrganizationId)
        {
            return WorkforceError.EmployeeNotFound();
        }

        var employments = await store.ListEmploymentsAsync(employeeId, cancellationToken);
        var employmentResult = ScheduleEmploymentResolver.ResolveCovering(employments, scheduleDate);
        if (!employmentResult.IsSuccess)
        {
            return employmentResult.Error!;
        }

        var workplace = await ScheduleWorkplaceResolver.ResolveAsync(
            store,
            employmentResult.Value!,
            scheduleDate,
            cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        if (!ScheduleAccess.AllowsWorkplace(
                scopedPropertyId,
                allowedDepartmentIds,
                workplace.Value!.Property.Id,
                workplace.Value.Department.Id))
        {
            return WorkforceError.SchedulePropertyAccessDenied();
        }

        var entry = await store.GetScheduleEntryAsync(
            workplace.Value!.Employment.Id,
            scheduleDate,
            cancellationToken);

        if (entry is null)
        {
            return ScheduleStateDto.Unscheduled(scheduleDate, workplace.Value);
        }

        ShiftDefinition? definition = null;
        if (entry.Kind == ScheduleEntryKind.Shift && entry.ShiftDefinitionId is { } shiftId)
        {
            definition = await store.GetShiftDefinitionAsync(shiftId, cancellationToken);
        }

        return await BuildStateAsync(store, entry, workplace.Value, definition, cancellationToken);
    }

    internal static async Task<WorkforceResult<ScheduleStateDto>> BuildStateAsync(
        IWorkforceStore store,
        ScheduleEntry entry,
        ScheduleWorkplaceContext workplace,
        ShiftDefinition? definition,
        CancellationToken cancellationToken)
    {
        if (entry.Kind == ScheduleEntryKind.RestDay)
        {
            return ScheduleStateDto.RestDay(entry, workplace);
        }

        if (definition is null && entry.ShiftDefinitionId is { } id)
        {
            definition = await store.GetShiftDefinitionAsync(id, cancellationToken);
        }

        if (definition is null)
        {
            return WorkforceError.ShiftDefinitionNotFound();
        }

        return ScheduleStateDto.Scheduled(entry, workplace, definition);
    }
}

public sealed class GetScheduleRangeQuery(IWorkforceStore store, IWorkplaceContext workplaceContext)
{
    public Task<WorkforceResult<IReadOnlyList<ScheduleRangeItemDto>>> ExecuteAsync(
        Guid employeeId,
        DateOnly from,
        DateOnly to,
        Guid? scopedPropertyId,
        CancellationToken cancellationToken) =>
        ExecuteAsync(employeeId, from, to, scopedPropertyId, allowedDepartmentIds: null, cancellationToken);

    public async Task<WorkforceResult<IReadOnlyList<ScheduleRangeItemDto>>> ExecuteAsync(
        Guid employeeId,
        DateOnly from,
        DateOnly to,
        Guid? scopedPropertyId,
        IReadOnlySet<Guid>? allowedDepartmentIds,
        CancellationToken cancellationToken)
    {
        if (!workplaceContext.HasOrganization)
        {
            return WorkforceError.WorkplaceNotConfigured();
        }

        if (to < from)
        {
            return WorkforceError.ScheduleValidationField(
                ScheduleValidation.Fields.To,
                ScheduleValidation.Codes.ScheduleInvalidRange,
                "Schedule range 'to' must be on or after 'from'.");
        }

        var employee = await store.GetEmployeeAsync(employeeId, cancellationToken);
        if (employee is null || employee.OrganizationId != workplaceContext.OrganizationId)
        {
            return WorkforceError.EmployeeNotFound();
        }

        var employments = await store.ListEmploymentsAsync(employeeId, cancellationToken);
        var employmentIds = employments.Select(item => item.Id).ToArray();
        var entries = await store.ListScheduleEntriesAsync(employmentIds, from, to, cancellationToken);

        var results = new List<ScheduleRangeItemDto>();
        foreach (var entry in entries.OrderBy(item => item.ScheduleDate))
        {
            var employment = employments.FirstOrDefault(item => item.Id == entry.EmploymentId);
            if (employment is null)
            {
                continue;
            }

            var workplace = await ScheduleWorkplaceResolver.ResolveAsync(
                store,
                employment,
                entry.ScheduleDate,
                cancellationToken);
            if (!workplace.IsSuccess)
            {
                // Prefer stored Assignment for historical display if re-resolve fails.
                var assignment = await store.GetAssignmentAsync(entry.AssignmentId, cancellationToken);
                if (assignment is null)
                {
                    continue;
                }

                var department = await store.GetDepartmentAsync(assignment.DepartmentId, cancellationToken);
                if (department is null)
                {
                    continue;
                }

                var property = await store.GetPropertyAsync(department.PropertyId, cancellationToken);
                if (property is null)
                {
                    continue;
                }

                workplace = WorkforceResult<ScheduleWorkplaceContext>.Success(
                    new ScheduleWorkplaceContext(employment, assignment, department, property));
            }

            // Authorize each row by Assignment historical Department/Property — never current workplace.
            if (!ScheduleAccess.AllowsWorkplace(
                    scopedPropertyId,
                    allowedDepartmentIds,
                    workplace.Value!.Property.Id,
                    workplace.Value.Department.Id))
            {
                continue;
            }

            ShiftDefinition? definition = null;
            if (entry.Kind == ScheduleEntryKind.Shift && entry.ShiftDefinitionId is { } shiftId)
            {
                definition = await store.GetShiftDefinitionAsync(shiftId, cancellationToken);
            }

            results.Add(ScheduleRangeItemDto.From(entry, workplace.Value!, definition));
        }

        return WorkforceResult<IReadOnlyList<ScheduleRangeItemDto>>.Success(results);
    }
}

public abstract record ScheduleStateDto(
    string State,
    DateOnly ScheduleDate,
    Guid? ScheduleEntryId,
    Guid EmploymentId,
    Guid AssignmentId,
    Guid DepartmentId,
    Guid PropertyId,
    string? PropertyName,
    string? Note)
{
    public static ScheduleStateDto Unscheduled(DateOnly scheduleDate, ScheduleWorkplaceContext workplace) =>
        new UnscheduledScheduleStateDto(
            "Unscheduled",
            scheduleDate,
            null,
            workplace.Employment.Id,
            workplace.Assignment.Id,
            workplace.Department.Id,
            workplace.Property.Id,
            workplace.Property.Name,
            null);

    public static ScheduleStateDto RestDay(ScheduleEntry entry, ScheduleWorkplaceContext workplace) =>
        new RestDayScheduleStateDto(
            "RestDay",
            entry.ScheduleDate,
            entry.Id,
            workplace.Employment.Id,
            entry.AssignmentId,
            workplace.Department.Id,
            workplace.Property.Id,
            workplace.Property.Name,
            entry.Note);

    public static ScheduleStateDto Scheduled(
        ScheduleEntry entry,
        ScheduleWorkplaceContext workplace,
        ShiftDefinition definition)
    {
        var interval = ShiftLocalInterval.From(entry.ScheduleDate, definition);
        return new ScheduledScheduleStateDto(
            "Scheduled",
            entry.ScheduleDate,
            entry.Id,
            workplace.Employment.Id,
            entry.AssignmentId,
            workplace.Department.Id,
            workplace.Property.Id,
            workplace.Property.Name,
            entry.Note,
            definition.Id,
            definition.Code,
            definition.Name,
            definition.IsActive,
            interval.StartLocalDate,
            interval.StartLocalTime,
            interval.EndLocalDate,
            interval.EndLocalTime,
            interval.EndsNextDay,
            interval.BreakMinutes,
            interval.GrossMinutes,
            interval.PlannedNetMinutes);
    }
}

public sealed record UnscheduledScheduleStateDto(
    string State,
    DateOnly ScheduleDate,
    Guid? ScheduleEntryId,
    Guid EmploymentId,
    Guid AssignmentId,
    Guid DepartmentId,
    Guid PropertyId,
    string? PropertyName,
    string? Note) : ScheduleStateDto(
    State,
    ScheduleDate,
    ScheduleEntryId,
    EmploymentId,
    AssignmentId,
    DepartmentId,
    PropertyId,
    PropertyName,
    Note);

public sealed record RestDayScheduleStateDto(
    string State,
    DateOnly ScheduleDate,
    Guid? ScheduleEntryId,
    Guid EmploymentId,
    Guid AssignmentId,
    Guid DepartmentId,
    Guid PropertyId,
    string? PropertyName,
    string? Note) : ScheduleStateDto(
    State,
    ScheduleDate,
    ScheduleEntryId,
    EmploymentId,
    AssignmentId,
    DepartmentId,
    PropertyId,
    PropertyName,
    Note);

public sealed record ScheduledScheduleStateDto(
    string State,
    DateOnly ScheduleDate,
    Guid? ScheduleEntryId,
    Guid EmploymentId,
    Guid AssignmentId,
    Guid DepartmentId,
    Guid PropertyId,
    string? PropertyName,
    string? Note,
    Guid ShiftDefinitionId,
    string ShiftCode,
    string ShiftName,
    bool ShiftIsActive,
    DateOnly StartLocalDate,
    TimeOnly StartLocalTime,
    DateOnly EndLocalDate,
    TimeOnly EndLocalTime,
    bool EndsNextDay,
    int BreakMinutes,
    int GrossMinutes,
    int PlannedNetMinutes) : ScheduleStateDto(
    State,
    ScheduleDate,
    ScheduleEntryId,
    EmploymentId,
    AssignmentId,
    DepartmentId,
    PropertyId,
    PropertyName,
    Note);

public sealed record ScheduleRangeItemDto(
    DateOnly ScheduleDate,
    string Kind,
    Guid ScheduleEntryId,
    Guid EmploymentId,
    Guid AssignmentId,
    Guid DepartmentId,
    Guid PropertyId,
    string? PropertyName,
    string? Note,
    Guid? ShiftDefinitionId,
    string? ShiftCode,
    string? ShiftName,
    DateOnly? StartLocalDate,
    TimeOnly? StartLocalTime,
    DateOnly? EndLocalDate,
    TimeOnly? EndLocalTime,
    bool? EndsNextDay,
    int? BreakMinutes,
    int? GrossMinutes,
    int? PlannedNetMinutes)
{
    public static ScheduleRangeItemDto From(
        ScheduleEntry entry,
        ScheduleWorkplaceContext workplace,
        ShiftDefinition? definition)
    {
        if (entry.Kind == ScheduleEntryKind.RestDay || definition is null)
        {
            return new ScheduleRangeItemDto(
                entry.ScheduleDate,
                entry.Kind.ToString(),
                entry.Id,
                entry.EmploymentId,
                entry.AssignmentId,
                workplace.Department.Id,
                workplace.Property.Id,
                workplace.Property.Name,
                entry.Note,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        var interval = ShiftLocalInterval.From(entry.ScheduleDate, definition);
        return new ScheduleRangeItemDto(
            entry.ScheduleDate,
            entry.Kind.ToString(),
            entry.Id,
            entry.EmploymentId,
            entry.AssignmentId,
            workplace.Department.Id,
            workplace.Property.Id,
            workplace.Property.Name,
            entry.Note,
            definition.Id,
            definition.Code,
            definition.Name,
            interval.StartLocalDate,
            interval.StartLocalTime,
            interval.EndLocalDate,
            interval.EndLocalTime,
            interval.EndsNextDay,
            interval.BreakMinutes,
            interval.GrossMinutes,
            interval.PlannedNetMinutes);
    }
}
