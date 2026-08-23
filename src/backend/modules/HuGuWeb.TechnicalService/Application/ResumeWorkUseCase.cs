using HuGuWeb.TechnicalService.Domain;

namespace HuGuWeb.TechnicalService.Application;

public sealed class ResumeWorkUseCase(
    ITechnicalServiceStore store,
    IAssignableEmployeeDirectory employees,
    IRoomIdentityDirectory rooms,
    ITechnicalServiceWorkplace workplace,
    ITechnicalServiceClock clock)
{
    public async Task<TechnicalServiceResult<MaintenanceIssueDetail>> ExecuteAsync(
        ResumeWorkCommand command,
        CancellationToken cancellationToken)
    {
        var loaded = await IssueCommandGuard.LoadAsync(store, workplace, command.IssueId, command.ExpectedVersion, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return loaded.Error!;
        }

        var issue = loaded.Value;
        if (!issue.TryResume(out var error))
        {
            return issue.Status == MaintenanceIssueStatus.UnableToResolve && issue.AssignedEmployeeId is null
                ? TechnicalServiceError.AssignmentRequired()
                : TechnicalServiceError.InvalidTransition(error ?? "Work could not be resumed.");
        }

        store.AddHistory(MaintenanceIssueHistoryEntry.Record(
            Guid.CreateVersion7(),
            issue.Id,
            clock.UtcNow,
            command.ActorUserId,
            MaintenanceIssueHistoryEvent.Resumed,
            fromStatus: MaintenanceIssueStatus.UnableToResolve,
            toStatus: MaintenanceIssueStatus.InProgress,
            toEmployeeId: issue.AssignedEmployeeId));

        return await IssueCommandGuard.SaveDetailAsync(store, employees, rooms, issue, cancellationToken);
    }
}

public sealed record ResumeWorkCommand(Guid IssueId, int ExpectedVersion, Guid ActorUserId);
