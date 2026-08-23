using HuGuWeb.TechnicalService.Domain;

namespace HuGuWeb.TechnicalService.Application;

public sealed class AssignIssueUseCase(
    ITechnicalServiceStore store,
    IAssignableEmployeeDirectory employees,
    IRoomIdentityDirectory rooms,
    ITechnicalServiceWorkplace workplace,
    ITechnicalServiceClock clock)
{
    public async Task<TechnicalServiceResult<MaintenanceIssueDetail>> ExecuteAsync(
        AssignIssueCommand command,
        CancellationToken cancellationToken)
    {
        var loaded = await IssueCommandGuard.LoadAsync(store, workplace, command.IssueId, command.ExpectedVersion, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return loaded.Error!;
        }

        var issue = loaded.Value;
        if (command.AssignedEmployeeId == Guid.Empty)
        {
            return TechnicalServiceError.AssignmentRequired();
        }

        var employee = await employees.FindAssignableAsync(command.AssignedEmployeeId, cancellationToken);
        if (employee is null)
        {
            return TechnicalServiceError.EmployeeNotFound();
        }

        var previous = issue.AssignedEmployeeId;
        if (!issue.TryAssign(employee.EmployeeId, out var error))
        {
            return TechnicalServiceError.InvalidTransition(error ?? "The issue could not be assigned.");
        }

        store.AddHistory(MaintenanceIssueHistoryEntry.Record(
            Guid.CreateVersion7(),
            issue.Id,
            clock.UtcNow,
            command.ActorUserId,
            previous is null ? MaintenanceIssueHistoryEvent.Assigned : MaintenanceIssueHistoryEvent.Reassigned,
            fromEmployeeId: previous,
            toEmployeeId: employee.EmployeeId));

        return await IssueCommandGuard.SaveDetailAsync(store, employees, rooms, issue, cancellationToken);
    }
}

public sealed record AssignIssueCommand(
    Guid IssueId,
    Guid AssignedEmployeeId,
    int ExpectedVersion,
    Guid ActorUserId);
