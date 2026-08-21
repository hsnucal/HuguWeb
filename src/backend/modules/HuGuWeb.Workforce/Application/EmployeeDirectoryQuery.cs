using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class EmployeeDirectoryQuery(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<IReadOnlyList<EmployeeDirectoryItem>>> ExecuteAsync(
        CancellationToken cancellationToken)
    {
        var workplace = await WorkplaceGuard.GetAsync(store, workplaceContext, cancellationToken);
        if (!workplace.IsSuccess)
        {
            return workplace.Error!;
        }

        var today = clock.Today;
        var employees = await store.ListEmployeesAsync(workplace.Value.Organization.Id, cancellationToken);
        var employments = await store.ListEmploymentsForEmployeesAsync(
            employees.Select(item => item.Id).ToArray(),
            cancellationToken);
        var assignments = await store.ListAssignmentsForEmploymentsAsync(
            employments.Select(item => item.Id).ToArray(),
            cancellationToken);
        var departments = (await store.ListDepartmentsAsync(workplace.Value.Property.Id, cancellationToken))
            .ToDictionary(item => item.Id);
        var positions = (await store.ListPositionsAsync(workplace.Value.Property.Id, cancellationToken))
            .ToDictionary(item => item.Id);

        var employmentsByEmployee = employments.ToLookup(item => item.EmployeeId);
        var assignmentsByEmployment = assignments.ToLookup(item => item.EmploymentId);

        var items = new List<EmployeeDirectoryItem>();
        foreach (var employee in employees.OrderBy(item => item.FamilyName).ThenBy(item => item.GivenName))
        {
            var latestEmployment = employmentsByEmployee[employee.Id]
                .OrderByDescending(item => item.StartDate)
                .FirstOrDefault();
            if (latestEmployment is null)
            {
                continue;
            }

            var status = latestEmployment.EffectiveStatus(today);
            var coveringDate = status == EmploymentStatus.Ended
                ? latestEmployment.EndDate ?? latestEmployment.StartDate
                : today;
            var assignment = PrimaryAssignments.Covering(
                assignmentsByEmployment[latestEmployment.Id].ToArray(),
                coveringDate);
            if (assignment is null && status != EmploymentStatus.Ended)
            {
                assignment = PrimaryAssignments.OrderedPrimaries(
                        assignmentsByEmployment[latestEmployment.Id].ToArray())
                    .LastOrDefault();
            }

            departments.TryGetValue(assignment?.DepartmentId ?? Guid.Empty, out var department);
            positions.TryGetValue(assignment?.PositionId ?? Guid.Empty, out var position);

            items.Add(new EmployeeDirectoryItem(
                employee.Id,
                employee.PersonnelNumber,
                employee.GivenName,
                employee.FamilyName,
                status,
                latestEmployment.StartDate,
                latestEmployment.EndDate,
                department?.Name,
                position?.Name));
        }

        return items;
    }
}

public sealed record EmployeeDirectoryItem(
    Guid EmployeeId,
    string PersonnelNumber,
    string GivenName,
    string FamilyName,
    EmploymentStatus EmploymentStatus,
    DateOnly EmploymentStartDate,
    DateOnly? EmploymentEndDate,
    string? DepartmentName,
    string? PositionName);
