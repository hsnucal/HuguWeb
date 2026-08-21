using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class ActiveWorkforceQuery(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<IReadOnlyList<ActiveWorkforceMember>>> ExecuteAsync(
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

        var members = new List<ActiveWorkforceMember>();
        foreach (var employee in employees.OrderBy(item => item.FamilyName).ThenBy(item => item.GivenName))
        {
            var currentEmployment = CurrentEmployment.TryFind(employmentsByEmployee[employee.Id]);
            if (currentEmployment is null)
            {
                continue;
            }

            if (currentEmployment.EffectiveStatus(today) != EmploymentStatus.Active)
            {
                continue;
            }

            var currentAssignment = PrimaryAssignments.Covering(
                assignmentsByEmployment[currentEmployment.Id].ToArray(),
                today);
            if (currentAssignment is null)
            {
                continue;
            }

            if (!departments.TryGetValue(currentAssignment.DepartmentId, out var department)
                || !positions.TryGetValue(currentAssignment.PositionId, out var position))
            {
                continue;
            }

            members.Add(new ActiveWorkforceMember(
                employee.Id,
                employee.PersonnelNumber,
                employee.GivenName,
                employee.FamilyName,
                currentEmployment.Id,
                currentEmployment.StartDate,
                department.Id,
                department.Name,
                position.Id,
                position.Name));
        }

        return members;
    }
}

public sealed record ActiveWorkforceMember(
    Guid EmployeeId,
    string PersonnelNumber,
    string GivenName,
    string FamilyName,
    Guid EmploymentId,
    DateOnly EmploymentStartDate,
    Guid DepartmentId,
    string DepartmentName,
    Guid PositionId,
    string PositionName);
