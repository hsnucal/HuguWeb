using HuGuWeb.TechnicalService.Domain;

namespace HuGuWeb.TechnicalService.Application;

public sealed class CreateIssueUseCase(
    ITechnicalServiceStore store,
    IAssignableEmployeeDirectory employees,
    IRoomIdentityDirectory rooms,
    ITechnicalServiceWorkplace workplace,
    ITechnicalServiceClock clock)
{
    public async Task<TechnicalServiceResult<MaintenanceIssueDetail>> ExecuteAsync(
        CreateIssueCommand command,
        CancellationToken cancellationToken)
    {
        var workplaceResult = WorkplaceGuard.Get(workplace);
        if (!workplaceResult.IsSuccess)
        {
            return workplaceResult.Error!;
        }

        if (!Enum.IsDefined(command.Priority))
        {
            return TechnicalServiceError.InvalidPriority();
        }

        if (!MaintenanceIssue.TryNormalizeBlocking(command.BlocksRoomUse, command.OutageClassification, out _))
        {
            return TechnicalServiceError.InvalidBlocking();
        }

        var room = await rooms.FindAsync(command.RoomId, cancellationToken);
        if (room is null || room.PropertyId != workplace.PropertyId)
        {
            return TechnicalServiceError.RoomNotFound();
        }

        if (!room.IsActive)
        {
            return TechnicalServiceError.RoomInactive();
        }

        var category = await store.GetCategoryAsync(command.CategoryId, cancellationToken);
        if (category is null || category.PropertyId != workplace.PropertyId || !category.IsActive)
        {
            return TechnicalServiceError.CategoryNotFound();
        }

        Guid? assignedEmployeeId = command.AssignedEmployeeId is { } assigned && assigned != Guid.Empty
            ? assigned
            : null;
        if (assignedEmployeeId is { } employeeId)
        {
            var employee = await employees.FindAssignableAsync(employeeId, cancellationToken);
            if (employee is null)
            {
                return TechnicalServiceError.EmployeeNotFound();
            }
        }

        Guid? reportedByEmployeeId = command.ReportedByEmployeeId is { } reporter && reporter != Guid.Empty
            ? reporter
            : null;
        if (reportedByEmployeeId is { } reporterId)
        {
            var known = await employees.GetEmployeesAsync([reporterId], cancellationToken);
            if (!known.ContainsKey(reporterId))
            {
                return TechnicalServiceError.EmployeeNotFound();
            }
        }

        if (!MaintenanceIssue.TryCreate(
                Guid.CreateVersion7(),
                workplace.PropertyId,
                room.RoomId,
                category.Id,
                command.Description,
                command.Priority,
                assignedEmployeeId,
                reportedByEmployeeId,
                command.OriginNote,
                command.BlocksRoomUse,
                command.OutageClassification,
                clock.UtcNow,
                out var issue,
                out var error)
            || issue is null)
        {
            return MapCreateError(error);
        }

        store.AddIssue(issue);
        store.AddHistory(MaintenanceIssueHistoryEntry.Record(
            Guid.CreateVersion7(),
            issue.Id,
            issue.CreatedAt,
            command.ActorUserId,
            MaintenanceIssueHistoryEvent.Created,
            toStatus: MaintenanceIssueStatus.Open,
            toPriority: issue.Priority,
            blocksRoomUse: issue.BlocksRoomUse,
            outageClassification: issue.OutageClassification,
            note: issue.Description));

        if (issue.AssignedEmployeeId is { } createdAssignee)
        {
            store.AddHistory(MaintenanceIssueHistoryEntry.Record(
                Guid.CreateVersion7(),
                issue.Id,
                issue.CreatedAt,
                command.ActorUserId,
                MaintenanceIssueHistoryEvent.Assigned,
                toEmployeeId: createdAssignee));
        }

        try
        {
            await store.SaveChangesAsync(cancellationToken);
        }
        catch (IssueConcurrencyConflictException)
        {
            return TechnicalServiceError.StaleIssue();
        }

        return await TechnicalServiceComposer.DetailAsync(store, employees, rooms, issue, cancellationToken);
    }

    private static TechnicalServiceError MapCreateError(string? error)
    {
        if (string.Equals(error, "Priority must be Normal, High, or Urgent.", StringComparison.Ordinal))
        {
            return TechnicalServiceError.InvalidPriority();
        }

        if (error is not null
            && (error.Contains("blocking", StringComparison.OrdinalIgnoreCase)
                || error.Contains("Outage", StringComparison.OrdinalIgnoreCase)))
        {
            return TechnicalServiceError.InvalidBlocking();
        }

        return TechnicalServiceError.InvalidRequest("invalid-issue", error ?? "The issue is invalid.");
    }
}

public sealed record CreateIssueCommand(
    Guid RoomId,
    Guid CategoryId,
    string Description,
    MaintenancePriority Priority,
    Guid? AssignedEmployeeId,
    Guid? ReportedByEmployeeId,
    string? OriginNote,
    bool BlocksRoomUse,
    OutageClassification? OutageClassification,
    Guid ActorUserId);
