using HuGuWeb.TechnicalService.Domain;

namespace HuGuWeb.TechnicalService.Application;

public sealed class ChangePriorityUseCase(
    ITechnicalServiceStore store,
    IAssignableEmployeeDirectory employees,
    IRoomIdentityDirectory rooms,
    ITechnicalServiceWorkplace workplace,
    ITechnicalServiceClock clock)
{
    public async Task<TechnicalServiceResult<MaintenanceIssueDetail>> ExecuteAsync(
        ChangePriorityCommand command,
        CancellationToken cancellationToken)
    {
        var loaded = await IssueCommandGuard.LoadAsync(store, workplace, command.IssueId, command.ExpectedVersion, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return loaded.Error!;
        }

        if (!Enum.IsDefined(command.Priority))
        {
            return TechnicalServiceError.InvalidPriority();
        }

        var issue = loaded.Value;
        var previous = issue.Priority;
        if (!issue.TryChangePriority(command.Priority, out var error))
        {
            return TechnicalServiceError.InvalidTransition(error ?? "Priority could not be changed.");
        }

        store.AddHistory(MaintenanceIssueHistoryEntry.Record(
            Guid.CreateVersion7(),
            issue.Id,
            clock.UtcNow,
            command.ActorUserId,
            MaintenanceIssueHistoryEvent.PriorityChanged,
            fromPriority: previous,
            toPriority: issue.Priority));

        return await IssueCommandGuard.SaveDetailAsync(store, employees, rooms, issue, cancellationToken);
    }
}

public sealed record ChangePriorityCommand(
    Guid IssueId,
    MaintenancePriority Priority,
    int ExpectedVersion,
    Guid ActorUserId);
