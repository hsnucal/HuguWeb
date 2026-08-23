using HuGuWeb.TechnicalService.Domain;

namespace HuGuWeb.TechnicalService.Application;

public sealed class MarkUnableToResolveUseCase(
    ITechnicalServiceStore store,
    IAssignableEmployeeDirectory employees,
    IRoomIdentityDirectory rooms,
    ITechnicalServiceWorkplace workplace,
    ITechnicalServiceClock clock)
{
    public async Task<TechnicalServiceResult<MaintenanceIssueDetail>> ExecuteAsync(
        MarkUnableToResolveCommand command,
        CancellationToken cancellationToken)
    {
        var loaded = await IssueCommandGuard.LoadAsync(store, workplace, command.IssueId, command.ExpectedVersion, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return loaded.Error!;
        }

        var issue = loaded.Value;
        if (!issue.TryMarkUnableToResolve(command.Note, out var error))
        {
            return string.IsNullOrWhiteSpace(command.Note)
                ? TechnicalServiceError.NoteRequired()
                : TechnicalServiceError.InvalidTransition(error ?? "Unable to resolve could not be recorded.");
        }

        store.AddHistory(MaintenanceIssueHistoryEntry.Record(
            Guid.CreateVersion7(),
            issue.Id,
            clock.UtcNow,
            command.ActorUserId,
            MaintenanceIssueHistoryEvent.UnableToResolve,
            fromStatus: MaintenanceIssueStatus.InProgress,
            toStatus: MaintenanceIssueStatus.UnableToResolve,
            note: issue.UnableToResolveNote));

        return await IssueCommandGuard.SaveDetailAsync(store, employees, rooms, issue, cancellationToken);
    }
}

public sealed record MarkUnableToResolveCommand(Guid IssueId, string Note, int ExpectedVersion, Guid ActorUserId);
