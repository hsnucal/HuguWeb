using HuGuWeb.TechnicalService.Domain;

namespace HuGuWeb.TechnicalService.Application;

internal static class TechnicalServiceComposer
{
    public static string NeededAction(MaintenanceIssue issue) =>
        issue.Status switch
        {
            MaintenanceIssueStatus.Open when issue.AssignedEmployeeId is null => "assign",
            MaintenanceIssueStatus.Open => "start",
            MaintenanceIssueStatus.InProgress => "resolve",
            MaintenanceIssueStatus.UnableToResolve => "resume",
            _ => "none"
        };

    public static async Task<MaintenanceIssueDetail> DetailAsync(
        ITechnicalServiceStore store,
        IAssignableEmployeeDirectory employees,
        IRoomIdentityDirectory rooms,
        MaintenanceIssue issue,
        CancellationToken cancellationToken)
    {
        var history = await store.ListHistoryAsync(issue.Id, cancellationToken);
        var roomIssues = await store.ListIssuesForRoomAsync(issue.RoomId, cancellationToken);
        var category = await store.GetCategoryAsync(issue.CategoryId, cancellationToken);
        var room = await rooms.FindAsync(issue.RoomId, cancellationToken);
        var names = await employees.GetEmployeesAsync(CollectEmployeeIds(issue, history), cancellationToken);

        return ToDetail(issue, room, category, roomIssues, history, names);
    }

    public static MaintenanceIssueDetail ToDetail(
        MaintenanceIssue issue,
        KnownRoom? room,
        MaintenanceIssueCategory? category,
        IReadOnlyList<MaintenanceIssue> roomIssues,
        IReadOnlyList<MaintenanceIssueHistoryEntry> history,
        IReadOnlyDictionary<Guid, AssignableEmployee> names)
    {
        return new MaintenanceIssueDetail(
            issue.Id,
            issue.RoomId,
            room?.Number ?? string.Empty,
            issue.Description,
            issue.CategoryId,
            category?.Name ?? string.Empty,
            issue.Priority,
            issue.Status,
            issue.AssignedEmployeeId,
            DisplayName(issue.AssignedEmployeeId, names),
            issue.ReportedByEmployeeId,
            DisplayName(issue.ReportedByEmployeeId, names),
            issue.OriginNote,
            issue.BlocksRoomUse,
            issue.OutageClassification,
            RoomServiceability.Derive(roomIssues),
            issue.ResolutionNote,
            issue.UnableToResolveNote,
            issue.PreparationImpact,
            issue.CreatedAt,
            issue.StartedAt,
            issue.ResolvedAt,
            issue.Version,
            NeededAction(issue),
            history
                .OrderBy(item => item.OccurredAt)
                .ThenBy(item => item.Id)
                .Select(item => new MaintenanceIssueHistoryItem(
                    item.Id,
                    item.EventType,
                    item.OccurredAt,
                    item.FromStatus,
                    item.ToStatus,
                    item.FromEmployeeId,
                    DisplayName(item.FromEmployeeId, names),
                    item.ToEmployeeId,
                    DisplayName(item.ToEmployeeId, names),
                    item.FromPriority,
                    item.ToPriority,
                    item.BlocksRoomUse,
                    item.OutageClassification,
                    item.PreparationImpact,
                    item.Note))
                .ToArray());
    }

    public static string DisplayName(Guid employeeId, string givenName, string familyName)
    {
        var name = $"{givenName} {familyName}".Trim();
        return string.IsNullOrWhiteSpace(name) ? employeeId.ToString() : name;
    }

    private static string? DisplayName(Guid? employeeId, IReadOnlyDictionary<Guid, AssignableEmployee> names)
    {
        if (employeeId is not { } id)
        {
            return null;
        }

        return names.TryGetValue(id, out var employee)
            ? DisplayName(employee.EmployeeId, employee.GivenName, employee.FamilyName)
            : null;
    }

    private static Guid[] CollectEmployeeIds(
        MaintenanceIssue issue,
        IReadOnlyList<MaintenanceIssueHistoryEntry> history)
    {
        return history
            .SelectMany(item => new[] { item.FromEmployeeId, item.ToEmployeeId })
            .Append(issue.AssignedEmployeeId)
            .Append(issue.ReportedByEmployeeId)
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();
    }
}
