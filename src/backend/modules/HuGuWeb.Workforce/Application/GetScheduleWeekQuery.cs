using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

/// <summary>
/// HR-06B week grid query. Property-local Monday–Sunday. Cell eligibility is presentation-only
/// (not a ScheduleEntry domain kind). Out-of-scope cells omit schedule details (no leakage).
/// </summary>
public sealed class GetScheduleWeekQuery(IWorkforceStore store, IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<ScheduleWeekDto>> ExecuteAsync(
        DateOnly weekStart,
        Guid? departmentId,
        Guid? scopedPropertyId,
        IReadOnlySet<Guid>? allowedDepartmentIds,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        if (weekStart.DayOfWeek != DayOfWeek.Monday)
        {
            return WorkforceError.ScheduleValidationField(
                ScheduleValidation.Fields.WeekStart,
                ScheduleValidation.Codes.ScheduleWeekStartInvalid,
                "Week start must be a Monday (Property-local DateOnly).");
        }

        var planningPropertyId = workplace.Value!.Property.Id;
        if (scopedPropertyId is { } scoped && scoped != planningPropertyId)
        {
            return WorkforceError.SchedulePropertyAccessDenied();
        }

        var departments = (await store.ListDepartmentsAsync(planningPropertyId, cancellationToken))
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var filterDepartments = departments
            .Where(item => ScheduleAccess.AllowsWorkplace(
                scopedPropertyId,
                allowedDepartmentIds,
                planningPropertyId,
                item.Id))
            .Select(item => new ScheduleWeekDepartmentDto(item.Id, item.Name, item.IsActive))
            .ToArray();

        HashSet<Guid>? selectedDepartmentIds = null;
        if (departmentId is { } selectedId)
        {
            if (filterDepartments.All(item => item.Id != selectedId))
            {
                return WorkforceError.ScheduleValidationField(
                    ScheduleValidation.Fields.DepartmentId,
                    ScheduleValidation.Codes.ScheduleDepartmentFilterDenied,
                    "The selected department is not available in the current schedule scope.");
            }

            selectedDepartmentIds = [selectedId];
        }
        else if (allowedDepartmentIds is not null)
        {
            // Department-scoped actors without an explicit pick: only their authorized departments.
            selectedDepartmentIds = allowedDepartmentIds.ToHashSet();
        }

        var weekEnd = weekStart.AddDays(6);
        var dates = Enumerable.Range(0, 7).Select(offset => weekStart.AddDays(offset)).ToArray();

        var employees = await store.ListEmployeesAsync(workplace.Value.Organization.Id, cancellationToken);
        if (employees.Count == 0)
        {
            return new ScheduleWeekDto(
                weekStart,
                weekEnd,
                dates,
                planningPropertyId,
                PropertyWide: allowedDepartmentIds is null,
                departmentId,
                filterDepartments,
                [],
                []);
        }

        var employeeIds = employees.Select(item => item.Id).ToArray();
        var employments = await store.ListEmploymentsForEmployeesAsync(employeeIds, cancellationToken);
        var employmentIds = employments.Select(item => item.Id).ToArray();
        var assignments = employmentIds.Length == 0
            ? Array.Empty<Assignment>()
            : await store.ListAssignmentsForEmploymentsAsync(employmentIds, cancellationToken);
        var entries = employmentIds.Length == 0
            ? Array.Empty<ScheduleEntry>()
            : await store.ListScheduleEntriesAsync(employmentIds, weekStart, weekEnd, cancellationToken);

        var departmentById = departments.ToDictionary(item => item.Id);
        var assignmentsByEmployment = assignments
            .GroupBy(item => item.EmploymentId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<Assignment>)group.ToArray());
        var employmentsByEmployee = employments
            .GroupBy(item => item.EmployeeId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<Employment>)group.ToArray());
        var entriesByEmploymentDate = entries.ToDictionary(
            item => (item.EmploymentId, item.ScheduleDate));

        var shiftDefinitionIds = entries
            .Where(item => item.Kind == ScheduleEntryKind.Shift && item.ShiftDefinitionId is not null)
            .Select(item => item.ShiftDefinitionId!.Value)
            .Distinct()
            .ToArray();
        var shiftDefinitions = new Dictionary<Guid, ShiftDefinition>();
        foreach (var shiftDefinitionId in shiftDefinitionIds)
        {
            var definition = await store.GetShiftDefinitionAsync(shiftDefinitionId, cancellationToken);
            if (definition is not null)
            {
                shiftDefinitions[definition.Id] = definition;
            }
        }

        var rows = new List<ScheduleWeekEmployeeDto>();
        foreach (var employee in employees.OrderBy(item => item.FamilyName, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(item => item.GivenName, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(item => item.PersonnelNumber, StringComparer.OrdinalIgnoreCase))
        {
            if (!employmentsByEmployee.TryGetValue(employee.Id, out var employeeEmployments))
            {
                continue;
            }

            var cells = new List<ScheduleWeekCellDto>(7);
            Guid? rowDepartmentId = null;
            string? rowDepartmentName = null;
            var includeRow = false;

            foreach (var date in dates)
            {
                var cell = BuildCell(
                    date,
                    employeeEmployments,
                    assignmentsByEmployment,
                    departmentById,
                    entriesByEmploymentDate,
                    shiftDefinitions,
                    planningPropertyId,
                    scopedPropertyId,
                    allowedDepartmentIds,
                    selectedDepartmentIds);

                cells.Add(cell.Cell);
                if (cell.IncludeInRow)
                {
                    includeRow = true;
                    if (rowDepartmentId is null && cell.Cell.DepartmentId is { } deptId)
                    {
                        rowDepartmentId = deptId;
                        rowDepartmentName = cell.Cell.DepartmentName;
                    }
                }
            }

            if (!includeRow)
            {
                continue;
            }

            rows.Add(new ScheduleWeekEmployeeDto(
                employee.Id,
                employee.GivenName,
                employee.FamilyName,
                employee.PersonnelNumber,
                rowDepartmentId,
                rowDepartmentName,
                cells));
        }

        var definitionDtos = shiftDefinitions.Values
            .OrderBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
            .Select(ScheduleWeekShiftDefinitionDto.From)
            .ToArray();

        return new ScheduleWeekDto(
            weekStart,
            weekEnd,
            dates,
            planningPropertyId,
            PropertyWide: allowedDepartmentIds is null,
            departmentId,
            filterDepartments,
            rows,
            definitionDtos);
    }

    private static (ScheduleWeekCellDto Cell, bool IncludeInRow) BuildCell(
        DateOnly date,
        IReadOnlyList<Employment> employments,
        IReadOnlyDictionary<Guid, IReadOnlyList<Assignment>> assignmentsByEmployment,
        IReadOnlyDictionary<Guid, Department> departmentById,
        IReadOnlyDictionary<(Guid EmploymentId, DateOnly ScheduleDate), ScheduleEntry> entriesByEmploymentDate,
        IReadOnlyDictionary<Guid, ShiftDefinition> shiftDefinitions,
        Guid planningPropertyId,
        Guid? scopedPropertyId,
        IReadOnlySet<Guid>? allowedDepartmentIds,
        IReadOnlySet<Guid>? selectedDepartmentIds)
    {
        var employmentResult = ScheduleEmploymentResolver.ResolveCovering(employments, date);
        if (!employmentResult.IsSuccess)
        {
            return (ScheduleWeekCellDto.NotEmployed(date), IncludeInRow: false);
        }

        var employment = employmentResult.Value!;
        if (!assignmentsByEmployment.TryGetValue(employment.Id, out var assignments))
        {
            return (ScheduleWeekCellDto.NotEmployed(date), IncludeInRow: false);
        }

        var assignment = EffectiveAssignmentResolver.ResolvePrimaryAssignmentOnDate(assignments, date);
        if (assignment is null || !departmentById.TryGetValue(assignment.DepartmentId, out var department))
        {
            return (ScheduleWeekCellDto.NotEmployed(date), IncludeInRow: false);
        }

        if (department.PropertyId != planningPropertyId)
        {
            return (ScheduleWeekCellDto.OutOfScope(date), IncludeInRow: false);
        }

        var authorized = ScheduleAccess.AllowsWorkplace(
            scopedPropertyId,
            allowedDepartmentIds,
            planningPropertyId,
            department.Id);
        if (!authorized)
        {
            // Never leak unauthorized schedule details; row inclusion comes from other authorized days.
            return (ScheduleWeekCellDto.OutOfScope(date), IncludeInRow: false);
        }

        var matchesFilter = selectedDepartmentIds is null || selectedDepartmentIds.Contains(department.Id);
        if (!matchesFilter)
        {
            // Employee belongs to another department today — out of scope for the selected filter.
            // Include row only if they match the filter on another day.
            return (ScheduleWeekCellDto.OutOfScope(date), IncludeInRow: false);
        }

        ScheduleEntry? entry = null;
        entriesByEmploymentDate.TryGetValue((employment.Id, date), out entry);

        if (entry is null)
        {
            return (ScheduleWeekCellDto.Unscheduled(
                date,
                employment.Id,
                assignment.Id,
                department.Id,
                department.Name), IncludeInRow: true);
        }

        if (entry.Kind == ScheduleEntryKind.RestDay)
        {
            return (ScheduleWeekCellDto.RestDay(
                date,
                entry,
                employment.Id,
                assignment.Id,
                department.Id,
                department.Name), IncludeInRow: true);
        }

        ShiftDefinition? definition = null;
        if (entry.ShiftDefinitionId is { } shiftId)
        {
            shiftDefinitions.TryGetValue(shiftId, out definition);
        }

        return (ScheduleWeekCellDto.Scheduled(
            date,
            entry,
            employment.Id,
            assignment.Id,
            department.Id,
            department.Name,
            definition), IncludeInRow: true);
    }
}

public sealed record ScheduleWeekDto(
    DateOnly WeekStart,
    DateOnly WeekEnd,
    IReadOnlyList<DateOnly> Dates,
    Guid PropertyId,
    bool PropertyWide,
    Guid? SelectedDepartmentId,
    IReadOnlyList<ScheduleWeekDepartmentDto> FilterDepartments,
    IReadOnlyList<ScheduleWeekEmployeeDto> Employees,
    IReadOnlyList<ScheduleWeekShiftDefinitionDto> ShiftDefinitions);

public sealed record ScheduleWeekDepartmentDto(Guid Id, string Name, bool IsActive);

public sealed record ScheduleWeekEmployeeDto(
    Guid EmployeeId,
    string GivenName,
    string FamilyName,
    string PersonnelNumber,
    Guid? RowDepartmentId,
    string? RowDepartmentName,
    IReadOnlyList<ScheduleWeekCellDto> Cells);

/// <summary>
/// Eligibility is UI/security context only — never persisted as ScheduleEntry.Kind.
/// State is null when Eligibility is not Editable.
/// </summary>
public sealed record ScheduleWeekCellDto(
    DateOnly Date,
    string Eligibility,
    string? State,
    Guid? ScheduleEntryId,
    Guid? EmploymentId,
    Guid? AssignmentId,
    Guid? DepartmentId,
    string? DepartmentName,
    string? Note,
    Guid? ShiftDefinitionId,
    string? ShiftCode,
    string? ShiftName,
    bool? ShiftIsActive,
    TimeOnly? StartLocalTime,
    TimeOnly? EndLocalTime,
    bool? EndsNextDay,
    int? BreakMinutes,
    int? GrossMinutes,
    int? PlannedNetMinutes)
{
    public const string EligibilityEditable = "Editable";
    public const string EligibilityOutOfScope = "OutOfScope";
    public const string EligibilityNotEmployed = "NotEmployed";

    public static ScheduleWeekCellDto NotEmployed(DateOnly date) =>
        new(date, EligibilityNotEmployed, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);

    public static ScheduleWeekCellDto OutOfScope(DateOnly date) =>
        new(date, EligibilityOutOfScope, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);

    public static ScheduleWeekCellDto Unscheduled(
        DateOnly date,
        Guid employmentId,
        Guid assignmentId,
        Guid departmentId,
        string departmentName) =>
        new(
            date,
            EligibilityEditable,
            "Unscheduled",
            null,
            employmentId,
            assignmentId,
            departmentId,
            departmentName,
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

    public static ScheduleWeekCellDto RestDay(
        DateOnly date,
        ScheduleEntry entry,
        Guid employmentId,
        Guid assignmentId,
        Guid departmentId,
        string departmentName) =>
        new(
            date,
            EligibilityEditable,
            "RestDay",
            entry.Id,
            employmentId,
            assignmentId,
            departmentId,
            departmentName,
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
            null);

    public static ScheduleWeekCellDto Scheduled(
        DateOnly date,
        ScheduleEntry entry,
        Guid employmentId,
        Guid assignmentId,
        Guid departmentId,
        string departmentName,
        ShiftDefinition? definition)
    {
        if (definition is null)
        {
            return new ScheduleWeekCellDto(
                date,
                EligibilityEditable,
                "Shift",
                entry.Id,
                employmentId,
                assignmentId,
                departmentId,
                departmentName,
                entry.Note,
                entry.ShiftDefinitionId,
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

        var interval = ShiftLocalInterval.From(date, definition);
        return new ScheduleWeekCellDto(
            date,
            EligibilityEditable,
            "Shift",
            entry.Id,
            employmentId,
            assignmentId,
            departmentId,
            departmentName,
            entry.Note,
            definition.Id,
            definition.Code,
            definition.Name,
            definition.IsActive,
            interval.StartLocalTime,
            interval.EndLocalTime,
            interval.EndsNextDay,
            interval.BreakMinutes,
            interval.GrossMinutes,
            interval.PlannedNetMinutes);
    }
}

public sealed record ScheduleWeekShiftDefinitionDto(
    Guid Id,
    string Code,
    string Name,
    TimeOnly StartLocalTime,
    TimeOnly EndLocalTime,
    bool EndsNextDay,
    int BreakMinutes,
    int GrossMinutes,
    int PlannedNetMinutes,
    bool IsActive)
{
    public static ScheduleWeekShiftDefinitionDto From(ShiftDefinition definition) =>
        new(
            definition.Id,
            definition.Code,
            definition.Name,
            definition.StartLocalTime,
            definition.EndLocalTime,
            definition.EndsNextDay,
            definition.BreakMinutes,
            definition.GrossMinutes,
            definition.PlannedNetMinutes,
            definition.IsActive);
}
