namespace HuGuWeb.TechnicalService.Application;

public sealed class ListAssignableEmployeesQuery(
    IAssignableEmployeeDirectory employees,
    ITechnicalServiceWorkplace workplace)
{
    public async Task<TechnicalServiceResult<IReadOnlyList<AssignableEmployeeItem>>> ExecuteAsync(
        CancellationToken cancellationToken)
    {
        var workplaceResult = WorkplaceGuard.Get(workplace);
        if (!workplaceResult.IsSuccess)
        {
            return workplaceResult.Error!;
        }

        var people = await employees.ListAssignableAsync(cancellationToken);
        return people
            .OrderBy(item => item.FamilyName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.GivenName, StringComparer.OrdinalIgnoreCase)
            .Select(item => new AssignableEmployeeItem(
                item.EmployeeId,
                item.GivenName,
                item.FamilyName,
                item.PersonnelNumber,
                TechnicalServiceComposer.DisplayName(item.EmployeeId, item.GivenName, item.FamilyName)))
            .ToArray();
    }
}
