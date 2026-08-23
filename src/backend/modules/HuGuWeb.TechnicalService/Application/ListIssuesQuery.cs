using HuGuWeb.TechnicalService.Domain;

namespace HuGuWeb.TechnicalService.Application;

public sealed class ListIssuesQuery(
    ITechnicalServiceStore store,
    IAssignableEmployeeDirectory employees,
    IRoomIdentityDirectory rooms,
    ITechnicalServiceWorkplace workplace)
{
    public async Task<TechnicalServiceResult<IReadOnlyList<MaintenanceIssueListItem>>> ExecuteAsync(
        CancellationToken cancellationToken)
    {
        var workplaceResult = WorkplaceGuard.Get(workplace);
        if (!workplaceResult.IsSuccess)
        {
            return workplaceResult.Error!;
        }

        var issues = await store.ListIssuesAsync(workplace.PropertyId, cancellationToken);
        var categories = (await store.ListCategoriesAsync(workplace.PropertyId, cancellationToken))
            .ToDictionary(item => item.Id);
        var roomMap = await rooms.GetAsync(issues.Select(item => item.RoomId).Distinct().ToArray(), cancellationToken);
        var names = await employees.GetEmployeesAsync(
            issues.Select(item => item.AssignedEmployeeId).Where(id => id is not null).Select(id => id!.Value).Distinct().ToArray(),
            cancellationToken);

        var roomIssues = issues.ToLookup(item => item.RoomId);
        var items = issues
            .OrderBy(StatusRank)
            .ThenBy(PriorityRank)
            .ThenBy(item => item.CreatedAt)
            .Select(issue =>
            {
                var room = roomMap.GetValueOrDefault(issue.RoomId);
                var category = categories.GetValueOrDefault(issue.CategoryId);
                var assignee = issue.AssignedEmployeeId is { } id && names.TryGetValue(id, out var employee)
                    ? TechnicalServiceComposer.DisplayName(employee.EmployeeId, employee.GivenName, employee.FamilyName)
                    : null;
                return new MaintenanceIssueListItem(
                    issue.Id,
                    issue.RoomId,
                    room?.Number ?? string.Empty,
                    issue.Description,
                    issue.CategoryId,
                    category?.Name ?? string.Empty,
                    issue.Priority,
                    issue.Status,
                    issue.AssignedEmployeeId,
                    assignee,
                    issue.BlocksRoomUse,
                    issue.OutageClassification,
                    RoomServiceability.Derive(roomIssues[issue.RoomId]),
                    issue.CreatedAt,
                    issue.Version,
                    TechnicalServiceComposer.NeededAction(issue));
            })
            .ToArray();

        return items;
    }

    private static int StatusRank(MaintenanceIssue issue) =>
        issue.Status switch
        {
            MaintenanceIssueStatus.UnableToResolve => 0,
            MaintenanceIssueStatus.InProgress => 1,
            MaintenanceIssueStatus.Open => 2,
            _ => 3
        };

    private static int PriorityRank(MaintenanceIssue issue) =>
        issue.Priority switch
        {
            MaintenancePriority.Urgent => 0,
            MaintenancePriority.High => 1,
            _ => 2
        };
}
