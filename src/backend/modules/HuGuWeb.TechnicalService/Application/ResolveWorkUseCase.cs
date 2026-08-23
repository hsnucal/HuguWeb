using HuGuWeb.TechnicalService.Domain;

namespace HuGuWeb.TechnicalService.Application;

public sealed class ResolveWorkUseCase(
    ITechnicalServiceStore store,
    IAssignableEmployeeDirectory employees,
    IRoomIdentityDirectory rooms,
    IRoomPreparationImpactConsumer preparation,
    ITechnicalServiceWorkplace workplace,
    ITechnicalServiceClock clock)
{
    public async Task<TechnicalServiceResult<MaintenanceIssueDetail>> ExecuteAsync(
        ResolveWorkCommand command,
        CancellationToken cancellationToken)
    {
        var loaded = await IssueCommandGuard.LoadAsync(store, workplace, command.IssueId, command.ExpectedVersion, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return loaded.Error!;
        }

        if (!Enum.IsDefined(command.PreparationImpact))
        {
            return TechnicalServiceError.InvalidPreparationImpact();
        }

        var issue = loaded.Value;
        var now = clock.UtcNow;
        if (!issue.TryResolve(command.Note, command.PreparationImpact, now, out var error))
        {
            return string.IsNullOrWhiteSpace(command.Note)
                ? TechnicalServiceError.NoteRequired()
                : TechnicalServiceError.InvalidTransition(error ?? "The issue could not be resolved.");
        }

        if (command.PreparationImpact == PreparationImpact.RequiresPreparation)
        {
            var consume = await preparation.EnsurePreparationRequiredAsync(
                issue.RoomId,
                command.ActorUserId,
                cancellationToken);
            if (!consume.IsSuccess)
            {
                return consume.Error!;
            }
        }

        store.AddHistory(MaintenanceIssueHistoryEntry.Record(
            Guid.CreateVersion7(),
            issue.Id,
            now,
            command.ActorUserId,
            MaintenanceIssueHistoryEvent.Resolved,
            fromStatus: MaintenanceIssueStatus.InProgress,
            toStatus: MaintenanceIssueStatus.Resolved,
            preparationImpact: issue.PreparationImpact,
            note: issue.ResolutionNote));

        return await IssueCommandGuard.SaveDetailAsync(store, employees, rooms, issue, cancellationToken);
    }
}

public sealed record ResolveWorkCommand(
    Guid IssueId,
    string Note,
    PreparationImpact PreparationImpact,
    int ExpectedVersion,
    Guid ActorUserId);
