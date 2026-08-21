using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class EmployeeHistoryQuery(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<EmployeeHistory>> ExecuteAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var employee = await store.GetEmployeeAsync(employeeId, cancellationToken);
        if (employee is null || employee.OrganizationId != workplace.Value.Organization.Id)
        {
            return WorkforceError.EmployeeNotFound();
        }

        var today = clock.Today;
        var employments = (await store.ListEmploymentsAsync(employee.Id, cancellationToken))
            .OrderByDescending(item => item.StartDate)
            .ToArray();
        var assignments = await store.ListAssignmentsForEmploymentsAsync(
            employments.Select(item => item.Id).ToArray(),
            cancellationToken);
        var departments = (await store.ListDepartmentsAsync(workplace.Value.Property.Id, cancellationToken))
            .ToDictionary(item => item.Id);
        var positions = (await store.ListPositionsAsync(workplace.Value.Property.Id, cancellationToken))
            .ToDictionary(item => item.Id);

        var employmentRecords = new List<EmploymentHistoryRecord>();
        foreach (var employment in employments)
        {
            var primaryAssignments = PrimaryAssignments.OrderedPrimaries(
                    assignments.Where(item => item.EmploymentId == employment.Id).ToArray())
                .Select(assignment => ToAssignmentRecord(assignment, departments, positions))
                .ToArray();

            employmentRecords.Add(new EmploymentHistoryRecord(
                employment.Id,
                employment.StartDate,
                employment.EndDate,
                employment.EffectiveStatus(today),
                primaryAssignments));
        }

        var currentEmployment = employmentRecords.FirstOrDefault(item => item.Status != EmploymentStatus.Ended);
        var currentAssignment = currentEmployment is null
            ? null
            : PrimaryAssignments.Covering(
                assignments.Where(item => item.EmploymentId == currentEmployment.Id).ToArray(),
                today);

        return new EmployeeHistory(
            employee.Id,
            employee.PersonnelNumber,
            employee.GivenName,
            employee.FamilyName,
            currentEmployment,
            currentAssignment is null ? null : ToAssignmentRecord(currentAssignment, departments, positions),
            employmentRecords);
    }

    private static AssignmentHistoryRecord ToAssignmentRecord(
        Assignment assignment,
        IReadOnlyDictionary<Guid, Department> departments,
        IReadOnlyDictionary<Guid, Position> positions)
    {
        departments.TryGetValue(assignment.DepartmentId, out var department);
        positions.TryGetValue(assignment.PositionId, out var position);
        return new AssignmentHistoryRecord(
            assignment.Id,
            assignment.DepartmentId,
            department?.Name ?? string.Empty,
            assignment.PositionId,
            position?.Name ?? string.Empty,
            assignment.StartDate,
            assignment.EndDate,
            assignment.Kind);
    }
}

public sealed record EmployeeHistory(
    Guid Id,
    string PersonnelNumber,
    string GivenName,
    string FamilyName,
    EmploymentHistoryRecord? CurrentEmployment,
    AssignmentHistoryRecord? CurrentPrimaryAssignment,
    IReadOnlyList<EmploymentHistoryRecord> Employments);

public sealed record EmploymentHistoryRecord(
    Guid Id,
    DateOnly StartDate,
    DateOnly? EndDate,
    EmploymentStatus Status,
    IReadOnlyList<AssignmentHistoryRecord> PrimaryAssignments);

public sealed record AssignmentHistoryRecord(
    Guid Id,
    Guid DepartmentId,
    string DepartmentName,
    Guid PositionId,
    string PositionName,
    DateOnly StartDate,
    DateOnly? EndDate,
    AssignmentKind Kind);
