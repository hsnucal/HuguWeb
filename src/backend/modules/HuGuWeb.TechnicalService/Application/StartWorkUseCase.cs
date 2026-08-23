using HuGuWeb.TechnicalService.Domain;

namespace HuGuWeb.TechnicalService.Application;

public sealed class StartWorkUseCase(
    ITechnicalServiceStore store,
    IAssignableEmployeeDirectory employees,
    IRoomIdentityDirectory rooms,
    ITechnicalServiceWorkplace workplace,
    ITechnicalServiceClock clock)
{
    public async Task<TechnicalServiceResult<MaintenanceIssueDetail>> ExecuteAsync(
        StartWorkCommand command,
        CancellationToken cancellationToken)
    {
        var loaded = await IssueCommandGuard.LoadAsync(store, workplace, command.IssueId, command.ExpectedVersion, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return loaded.Error!;
        }

        var issue = loaded.Value;
        var now = clock.UtcNow;
        if (!issue.TryStart(now, out var error))
        {
            return issue.Status == MaintenanceIssueStatus.Open && issue.AssignedEmployeeId is null
                ? TechnicalServiceError.AssignmentRequired()
                : TechnicalServiceError.InvalidTransition(error ?? "Work could not be started.");
        }

        store.AddHistory(MaintenanceIssueHistoryEntry.Record(
            Guid.CreateVersion7(),
            issue.Id,
            now,
            command.ActorUserId,
            MaintenanceIssueHistoryEvent.Started,
            fromStatus: MaintenanceIssueStatus.Open,
            toStatus: MaintenanceIssueStatus.InProgress,
            toEmployeeId: issue.AssignedEmployeeId));

        return await IssueCommandGuard.SaveDetailAsync(store, employees, rooms, issue, cancellationToken);
    }
}

public sealed record StartWorkCommand(Guid IssueId, int ExpectedVersion, Guid ActorUserId);
