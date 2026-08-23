using HuGuWeb.TechnicalService.Application;
using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.TechnicalService.Infrastructure;

public sealed class WorkforceAssignableEmployeeDirectory(
    IWorkforceStore store,
    IWorkforceClock clock,
    IWorkplaceContext workplace) : IAssignableEmployeeDirectory
{
    public async Task<AssignableEmployee?> FindAssignableAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var employee = await store.GetEmployeeAsync(employeeId, cancellationToken);
        if (employee is null || employee.OrganizationId != workplace.OrganizationId)
        {
            return null;
        }

        if (!await IsCurrentlyEmployedAsync(employee.Id, cancellationToken))
        {
            return null;
        }

        return ToAssignable(employee);
    }

    public async Task<IReadOnlyList<AssignableEmployee>> ListAssignableAsync(CancellationToken cancellationToken)
    {
        if (!workplace.IsConfigured)
        {
            return [];
        }

        var people = await store.ListEmployeesAsync(workplace.OrganizationId, cancellationToken);
        var result = new List<AssignableEmployee>();
        foreach (var employee in people)
        {
            if (await IsCurrentlyEmployedAsync(employee.Id, cancellationToken))
            {
                result.Add(ToAssignable(employee));
            }
        }

        return result;
    }

    public async Task<IReadOnlyDictionary<Guid, AssignableEmployee>> GetEmployeesAsync(
        IReadOnlyCollection<Guid> employeeIds,
        CancellationToken cancellationToken)
    {
        if (employeeIds.Count == 0)
        {
            return new Dictionary<Guid, AssignableEmployee>();
        }

        var people = await store.ListEmployeesAsync(workplace.OrganizationId, cancellationToken);
        return people
            .Where(item => employeeIds.Contains(item.Id))
            .ToDictionary(item => item.Id, ToAssignable);
    }

    private async Task<bool> IsCurrentlyEmployedAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        var employments = await store.ListEmploymentsAsync(employeeId, cancellationToken);
        var open = employments.Where(item => !item.IsEnded).Take(2).ToArray();
        return open.Length == 1 && open[0].EffectiveStatus(clock.Today) == EmploymentStatus.Active;
    }

    private static AssignableEmployee ToAssignable(Employee employee) =>
        new(employee.Id, employee.GivenName, employee.FamilyName, employee.PersonnelNumber);
}
