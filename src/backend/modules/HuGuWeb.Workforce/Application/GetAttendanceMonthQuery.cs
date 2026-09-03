using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

/// <summary>
/// Monthly Puantaj read. Batch-loads overlapping employments, assignments, schedule, recorded leave,
/// and sparse corrections, then resolves in memory. Property comes from workplace context.
/// </summary>
public sealed class GetAttendanceMonthQuery(IWorkforceStore store, IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<AttendanceMonthDto>> ExecuteAsync(
        int year,
        int month,
        Guid? departmentId,
        string? search,
        Guid? scopedPropertyId,
        IReadOnlySet<Guid>? allowedDepartmentIds,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        if (month is < 1 or > 12 || year is < 1 or > 9999)
        {
            return WorkforceError.AttendanceValidationField(
                AttendanceValidation.Fields.Month,
                AttendanceValidation.Codes.AttendanceInvalidMonth,
                "Year and month must form a valid calendar month.");
        }

        var planningPropertyId = workplace.Value!.Property.Id;
        if (scopedPropertyId is { } scoped && scoped != planningPropertyId)
        {
            return WorkforceError.AttendancePropertyAccessDenied();
        }

        DateOnly monthStart;
        DateOnly monthEnd;
        try
        {
            monthStart = new DateOnly(year, month, 1);
            monthEnd = monthStart.AddMonths(1).AddDays(-1);
        }
        catch (ArgumentOutOfRangeException)
        {
            return WorkforceError.AttendanceValidationField(
                AttendanceValidation.Fields.Month,
                AttendanceValidation.Codes.AttendanceInvalidMonth,
                "Year and month must form a valid calendar month.");
        }

        var dates = Enumerable.Range(0, monthEnd.Day).Select(offset => monthStart.AddDays(offset)).ToArray();

        var departments = (await store.ListDepartmentsAsync(planningPropertyId, cancellationToken))
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var filterDepartments = departments
            .Where(item => ScheduleAccess.AllowsWorkplace(
                scopedPropertyId,
                allowedDepartmentIds,
                planningPropertyId,
                item.Id))
            .Select(item => new AttendanceMonthDepartmentDto(item.Id, item.Name, item.IsActive))
            .ToArray();

        HashSet<Guid>? selectedDepartmentIds = null;
        if (departmentId is { } selectedId)
        {
            if (filterDepartments.All(item => item.Id != selectedId))
            {
                return WorkforceError.AttendanceValidationField(
                    AttendanceValidation.Fields.DepartmentId,
                    AttendanceValidation.Codes.AttendanceDepartmentFilterDenied,
                    "The selected department is not available in the current attendance scope.");
            }

            selectedDepartmentIds = [selectedId];
        }
        else if (allowedDepartmentIds is not null)
        {
            selectedDepartmentIds = allowedDepartmentIds.ToHashSet();
        }

        var employees = await store.ListEmployeesAsync(workplace.Value.Organization.Id, cancellationToken);
        employees = FilterSearch(employees, search);
        if (employees.Count == 0)
        {
            return EmptyMonth(
                year,
                month,
                monthStart,
                monthEnd,
                dates,
                planningPropertyId,
                allowedDepartmentIds is null,
                departmentId,
                filterDepartments);
        }

        var employeeIds = employees.Select(item => item.Id).ToArray();
        var employments = await store.ListEmploymentsForEmployeesAsync(employeeIds, cancellationToken);
        var overlapping = employments
            .Where(item => OverlapsMonth(item, monthStart, monthEnd))
            .ToArray();
        var overlappingIds = overlapping.Select(item => item.Id).ToArray();
        if (overlappingIds.Length == 0)
        {
            return EmptyMonth(
                year,
                month,
                monthStart,
                monthEnd,
                dates,
                planningPropertyId,
                allowedDepartmentIds is null,
                departmentId,
                filterDepartments);
        }

        var assignments = await store.ListAssignmentsForEmploymentsAsync(overlappingIds, cancellationToken);
        var entries = await store.ListScheduleEntriesAsync(overlappingIds, monthStart, monthEnd, cancellationToken);
        var leaves = await store.ListRecordedLeaveRecordsOverlappingAsync(
            overlappingIds,
            monthStart,
            monthEnd,
            cancellationToken);
        var corrections = await store.ListAttendanceCorrectionsAsync(
            overlappingIds,
            monthStart,
            monthEnd,
            cancellationToken);
        var shiftDefinitions = (await store.ListShiftDefinitionsAsync(planningPropertyId, cancellationToken))
            .ToDictionary(item => item.Id);
        var leaveTypes = (await store.ListLeaveTypesAsync(workplace.Value.Organization.Id, cancellationToken))
            .ToDictionary(item => item.Id);

        var departmentById = departments.ToDictionary(item => item.Id);
        var assignmentsByEmployment = assignments
            .GroupBy(item => item.EmploymentId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<Assignment>)group.ToArray());
        var employmentsByEmployee = overlapping
            .GroupBy(item => item.EmployeeId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<Employment>)group.ToArray());
        var entriesByKey = entries.ToDictionary(item => (item.EmploymentId, item.ScheduleDate));
        var correctionsByKey = corrections.ToDictionary(item => (item.EmploymentId, item.LocalDate));
        var leavesByEmployment = leaves
            .GroupBy(item => item.EmploymentId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<LeaveRecord>)group.ToArray());

        var rows = new List<AttendanceMonthEmployeeDto>();
        foreach (var employee in employees.OrderBy(item => item.FamilyName, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(item => item.GivenName, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(item => item.PersonnelNumber, StringComparer.OrdinalIgnoreCase))
        {
            if (!employmentsByEmployee.TryGetValue(employee.Id, out var employeeEmployments))
            {
                continue;
            }

            var days = new List<AttendanceDayResult>(dates.Length);
            Guid? rowDepartmentId = null;
            string? rowDepartmentName = null;
            Guid? rowEmploymentId = null;
            var includeRow = false;

            foreach (var date in dates)
            {
                var day = BuildDay(
                    date,
                    employeeEmployments,
                    assignmentsByEmployment,
                    departmentById,
                    entriesByKey,
                    correctionsByKey,
                    leavesByEmployment,
                    shiftDefinitions,
                    leaveTypes,
                    planningPropertyId,
                    scopedPropertyId,
                    allowedDepartmentIds,
                    selectedDepartmentIds);

                days.Add(day.Result);
                if (day.IncludeInRow)
                {
                    includeRow = true;
                    if (rowDepartmentId is null && day.Result.DepartmentId is { } deptId)
                    {
                        rowDepartmentId = deptId;
                        rowDepartmentName = day.Result.DepartmentName;
                    }

                    rowEmploymentId ??= day.Result.EmploymentId;
                }
            }

            if (!includeRow)
            {
                continue;
            }

            rows.Add(new AttendanceMonthEmployeeDto(
                employee.Id,
                rowEmploymentId,
                employee.GivenName,
                employee.FamilyName,
                employee.PersonnelNumber,
                rowDepartmentId,
                rowDepartmentName,
                days,
                AttendanceMonthTotalsDto.From(days)));
        }

        return new AttendanceMonthDto(
            year,
            month,
            monthStart,
            monthEnd,
            dates,
            planningPropertyId,
            PropertyWide: allowedDepartmentIds is null,
            departmentId,
            filterDepartments,
            rows);
    }

    private static (AttendanceDayResult Result, bool IncludeInRow) BuildDay(
        DateOnly date,
        IReadOnlyList<Employment> employments,
        IReadOnlyDictionary<Guid, IReadOnlyList<Assignment>> assignmentsByEmployment,
        IReadOnlyDictionary<Guid, Department> departmentById,
        IReadOnlyDictionary<(Guid EmploymentId, DateOnly Date), ScheduleEntry> entriesByKey,
        IReadOnlyDictionary<(Guid EmploymentId, DateOnly Date), AttendanceCorrection> correctionsByKey,
        IReadOnlyDictionary<Guid, IReadOnlyList<LeaveRecord>> leavesByEmployment,
        IReadOnlyDictionary<Guid, ShiftDefinition> shiftDefinitions,
        IReadOnlyDictionary<Guid, LeaveType> leaveTypes,
        Guid planningPropertyId,
        Guid? scopedPropertyId,
        IReadOnlySet<Guid>? allowedDepartmentIds,
        IReadOnlySet<Guid>? selectedDepartmentIds)
    {
        var employmentResult = ScheduleEmploymentResolver.ResolveCovering(employments, date);
        if (!employmentResult.IsSuccess)
        {
            return (AttendanceDayComposer.NotEmployed(date), IncludeInRow: false);
        }

        var employment = employmentResult.Value!;
        if (!assignmentsByEmployment.TryGetValue(employment.Id, out var assignments))
        {
            return (AttendanceDayComposer.NotEmployed(date), IncludeInRow: false);
        }

        var assignment = EffectiveAssignmentResolver.ResolvePrimaryAssignmentOnDate(assignments, date);
        if (assignment is null || !departmentById.TryGetValue(assignment.DepartmentId, out var department))
        {
            return (AttendanceDayComposer.OutOfScope(date), IncludeInRow: false);
        }

        if (department.PropertyId != planningPropertyId)
        {
            return (AttendanceDayComposer.OutOfScope(date), IncludeInRow: false);
        }

        if (!ScheduleAccess.AllowsWorkplace(
                scopedPropertyId,
                allowedDepartmentIds,
                planningPropertyId,
                department.Id))
        {
            return (AttendanceDayComposer.OutOfScope(date), IncludeInRow: false);
        }

        if (selectedDepartmentIds is not null && !selectedDepartmentIds.Contains(department.Id))
        {
            return (AttendanceDayComposer.OutOfScope(date), IncludeInRow: false);
        }

        entriesByKey.TryGetValue((employment.Id, date), out var schedule);
        correctionsByKey.TryGetValue((employment.Id, date), out var correction);
        leavesByEmployment.TryGetValue(employment.Id, out var employmentLeaves);
        var leave = AttendanceDayResolver.SelectCoveringRecordedLeave(
            employmentLeaves ?? Array.Empty<LeaveRecord>(),
            date);

        ShiftDefinition? shiftDefinition = null;
        if (schedule is { Kind: ScheduleEntryKind.Shift, ShiftDefinitionId: { } shiftId })
        {
            shiftDefinitions.TryGetValue(shiftId, out shiftDefinition);
        }

        LeaveType? leaveType = null;
        if (leave is not null)
        {
            leaveTypes.TryGetValue(leave.LeaveTypeId, out leaveType);
        }

        var resolution = AttendanceDayResolver.ResolveInEmployment(date, correction, leave, schedule);
        return (AttendanceDayComposer.FromResolution(
            resolution,
            employment.Id,
            assignment.Id,
            department.Id,
            department.Name,
            shiftDefinition,
            leaveType), IncludeInRow: true);
    }

    private static bool OverlapsMonth(Employment employment, DateOnly monthStart, DateOnly monthEnd) =>
        employment.StartDate <= monthEnd && (employment.EndDate is null || employment.EndDate >= monthStart);

    private static IReadOnlyList<Employee> FilterSearch(IReadOnlyList<Employee> employees, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return employees;
        }

        var term = search.Trim();
        return employees
            .Where(item =>
                item.GivenName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || item.FamilyName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || item.PersonnelNumber.Contains(term, StringComparison.OrdinalIgnoreCase)
                || $"{item.GivenName} {item.FamilyName}".Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static AttendanceMonthDto EmptyMonth(
        int year,
        int month,
        DateOnly monthStart,
        DateOnly monthEnd,
        IReadOnlyList<DateOnly> dates,
        Guid propertyId,
        bool propertyWide,
        Guid? selectedDepartmentId,
        IReadOnlyList<AttendanceMonthDepartmentDto> filterDepartments) =>
        new(
            year,
            month,
            monthStart,
            monthEnd,
            dates,
            propertyId,
            propertyWide,
            selectedDepartmentId,
            filterDepartments,
            []);
}

public sealed record AttendanceMonthDto(
    int Year,
    int Month,
    DateOnly MonthStart,
    DateOnly MonthEnd,
    IReadOnlyList<DateOnly> Dates,
    Guid PropertyId,
    bool PropertyWide,
    Guid? SelectedDepartmentId,
    IReadOnlyList<AttendanceMonthDepartmentDto> FilterDepartments,
    IReadOnlyList<AttendanceMonthEmployeeDto> Employees);

public sealed record AttendanceMonthDepartmentDto(Guid Id, string Name, bool IsActive);

public sealed record AttendanceMonthEmployeeDto(
    Guid EmployeeId,
    Guid? EmploymentId,
    string GivenName,
    string FamilyName,
    string PersonnelNumber,
    Guid? RowDepartmentId,
    string? RowDepartmentName,
    IReadOnlyList<AttendanceDayResult> Days,
    AttendanceMonthTotalsDto Totals);

public sealed record AttendanceMonthTotalsDto(
    int WorkedDays,
    int LeaveDays,
    int RestDays,
    int AbsentDays,
    int UnresolvedDays,
    int PlannedMinutes)
{
    public static AttendanceMonthTotalsDto From(IEnumerable<AttendanceDayResult> days)
    {
        var inEmployment = days
            .Where(item => item.Coverage == nameof(AttendanceCoverage.InEmployment))
            .ToArray();
        return new AttendanceMonthTotalsDto(
            inEmployment.Count(item => item.AcceptedKind == nameof(AttendanceAcceptedKind.Worked)),
            inEmployment.Count(item => item.AcceptedKind == nameof(AttendanceAcceptedKind.Leave)),
            inEmployment.Count(item => item.AcceptedKind == nameof(AttendanceAcceptedKind.RestDay)),
            inEmployment.Count(item => item.AcceptedKind == nameof(AttendanceAcceptedKind.Absent)),
            inEmployment.Count(item => item.IsUnresolved),
            inEmployment.Sum(item => item.PlannedMinutes ?? 0));
    }
}

public sealed record AttendanceDayResult(
    DateOnly LocalDate,
    string Coverage,
    string? AcceptedKind,
    string? Source,
    bool IsProvisional,
    bool IsManual,
    bool IsUnresolved,
    string? CorrectionReason,
    Guid? EmploymentId,
    Guid? AssignmentId,
    Guid? DepartmentId,
    string? DepartmentName,
    AttendanceDayScheduleDto? Schedule,
    AttendanceDayLeaveDto? Leave,
    int? PlannedMinutes,
    int? AcceptedWorkedMinutes)
{
    public static AttendanceDayResult NotEmployed(DateOnly date) =>
        new(
            date,
            nameof(AttendanceCoverage.NotEmployed),
            null,
            null,
            false,
            false,
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

    public static AttendanceDayResult OutOfScope(DateOnly date) =>
        new(
            date,
            nameof(AttendanceCoverage.OutOfScope),
            null,
            null,
            false,
            false,
            false,
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

public sealed record AttendanceDayScheduleDto(
    string State,
    Guid? ScheduleEntryId,
    Guid? ShiftDefinitionId,
    string? ShiftCode,
    string? ShiftName,
    TimeOnly? StartLocalTime,
    TimeOnly? EndLocalTime,
    bool? EndsNextDay)
{
    public static AttendanceDayScheduleDto Unscheduled() =>
        new("Unscheduled", null, null, null, null, null, null, null);

    public static AttendanceDayScheduleDto RestDay(Guid scheduleEntryId) =>
        new("RestDay", scheduleEntryId, null, null, null, null, null, null);

    public static AttendanceDayScheduleDto ShiftMissingDefinition(ScheduleEntry entry) =>
        new("Shift", entry.Id, entry.ShiftDefinitionId, null, null, null, null, null);

    public static AttendanceDayScheduleDto Shift(
        ScheduleEntry entry,
        ShiftDefinition definition,
        ShiftLocalInterval interval) =>
        new(
            "Shift",
            entry.Id,
            definition.Id,
            definition.Code,
            definition.Name,
            interval.StartLocalTime,
            interval.EndLocalTime,
            interval.EndsNextDay);
}

public sealed record AttendanceDayLeaveDto(
    Guid LeaveRecordId,
    Guid LeaveTypeId,
    string? LeaveTypeCode,
    string? LeaveTypeName,
    LeaveTypeSystemKind? SystemKind,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Amount);
