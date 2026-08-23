namespace HuGuWeb.RoomOperations.Application;

public sealed class ListAssignableEmployeesQuery(IAssignableEmployeeDirectory employees)
{
    public async Task<RoomOperationsResult<IReadOnlyList<AssignableEmployeeItem>>> ExecuteAsync(
        CancellationToken cancellationToken)
    {
        var people = await employees.ListAssignableAsync(cancellationToken);
        return people
            .OrderBy(item => item.FamilyName)
            .ThenBy(item => item.GivenName)
            .Select(item => new AssignableEmployeeItem(
                item.EmployeeId,
                item.GivenName,
                item.FamilyName,
                item.PersonnelNumber,
                $"{item.GivenName} {item.FamilyName}".Trim()))
            .ToArray();
    }
}
