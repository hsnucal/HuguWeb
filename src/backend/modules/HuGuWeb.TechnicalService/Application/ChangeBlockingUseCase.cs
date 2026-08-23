using HuGuWeb.TechnicalService.Domain;

namespace HuGuWeb.TechnicalService.Application;

public sealed class ChangeBlockingUseCase(
    ITechnicalServiceStore store,
    IAssignableEmployeeDirectory employees,
    IRoomIdentityDirectory rooms,
    ITechnicalServiceWorkplace workplace,
    ITechnicalServiceClock clock)
{
    public async Task<TechnicalServiceResult<MaintenanceIssueDetail>> ExecuteAsync(
        ChangeBlockingCommand command,
        CancellationToken cancellationToken)
    {
        var loaded = await IssueCommandGuard.LoadAsync(store, workplace, command.IssueId, command.ExpectedVersion, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return loaded.Error!;
        }

        if (!MaintenanceIssue.TryNormalizeBlocking(command.BlocksRoomUse, command.OutageClassification, out _))
        {
            return TechnicalServiceError.InvalidBlocking();
        }

        var issue = loaded.Value;
        if (!issue.TryChangeBlocking(command.BlocksRoomUse, command.OutageClassification, out var error))
        {
            return TechnicalServiceError.InvalidTransition(error ?? "Blocking could not be changed.");
        }

        store.AddHistory(MaintenanceIssueHistoryEntry.Record(
            Guid.CreateVersion7(),
            issue.Id,
            clock.UtcNow,
            command.ActorUserId,
            MaintenanceIssueHistoryEvent.BlockingChanged,
            blocksRoomUse: issue.BlocksRoomUse,
            outageClassification: issue.OutageClassification));

        return await IssueCommandGuard.SaveDetailAsync(store, employees, rooms, issue, cancellationToken);
    }
}

public sealed record ChangeBlockingCommand(
    Guid IssueId,
    bool BlocksRoomUse,
    OutageClassification? OutageClassification,
    int ExpectedVersion,
    Guid ActorUserId);
