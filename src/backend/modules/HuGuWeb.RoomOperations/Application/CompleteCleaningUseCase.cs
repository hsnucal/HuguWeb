using HuGuWeb.RoomOperations.Domain;

namespace HuGuWeb.RoomOperations.Application;

public sealed class CompleteCleaningUseCase(
    IRoomOperationsStore store,
    IAssignableEmployeeDirectory employees,
    IRoomServiceabilityLookup serviceability,
    IRoomOperationsWorkplace workplace,
    IRoomOperationsClock clock)
{
    public async Task<RoomOperationsResult<RoomOperationsDetail>> ExecuteAsync(
        CompleteCleaningCommand command,
        CancellationToken cancellationToken)
    {
        var workplaceResult = WorkplaceGuard.Get(workplace);
        if (!workplaceResult.IsSuccess)
        {
            return workplaceResult.Error!;
        }

        var work = await store.GetWorkItemAsync(command.WorkItemId, cancellationToken);
        if (work is null)
        {
            return RoomOperationsError.WorkItemNotFound();
        }

        var room = await store.GetRoomAsync(work.RoomId, cancellationToken);
        if (room is null || room.PropertyId != workplace.PropertyId)
        {
            return RoomOperationsError.RoomNotFound();
        }

        if (!work.IsOpen)
        {
            return RoomOperationsError.WorkItemNotCurrent();
        }

        if (work.ReadinessCycleId != room.ReadinessCycleId)
        {
            return RoomOperationsError.StaleWorkItem();
        }

        var openWork = await store.FindOpenWorkAsync(room.Id, cancellationToken);
        if (openWork is null || openWork.Id != work.Id)
        {
            return RoomOperationsError.WorkItemNotCurrent();
        }

        if (!work.TryComplete(work.AssignedEmployeeId, clock.UtcNow, room.ReadinessCycleId, out var completeError))
        {
            return completeError == "This work item is not current for the room."
                ? RoomOperationsError.StaleWorkItem()
                : RoomOperationsError.WorkItemNotCurrent();
        }

        if (!room.TryMarkClean(work.ReadinessCycleId, out var cleanError))
        {
            return cleanError == "This work item is not current for the room."
                ? RoomOperationsError.StaleWorkItem()
                : RoomOperationsError.InvalidReadinessTransition(cleanError ?? "The room could not become Clean.");
        }

        store.AddHistory(RoomReadinessHistoryEntry.Record(
            Guid.CreateVersion7(),
            room.Id,
            room.ReadinessCycleId,
            RoomReadiness.Clean,
            ReadinessChangeCause.CleaningCompleted,
            clock.UtcNow,
            command.ActorUserId,
            work.AssignedEmployeeId,
            work.Id));

        await store.SaveChangesAsync(cancellationToken);
        return await RoomOperationsComposer.DetailAsync(store, employees, serviceability, room, cancellationToken);
    }
}

public sealed record CompleteCleaningCommand(Guid WorkItemId, Guid ActorUserId);
