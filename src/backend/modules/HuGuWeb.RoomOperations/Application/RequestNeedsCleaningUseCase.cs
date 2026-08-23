using HuGuWeb.RoomOperations.Domain;

namespace HuGuWeb.RoomOperations.Application;

public sealed class RequestNeedsCleaningUseCase(
    IRoomOperationsStore store,
    IAssignableEmployeeDirectory employees,
    IRoomOperationsWorkplace workplace,
    IRoomOperationsClock clock)
{
    public async Task<RoomOperationsResult<RoomOperationsDetail>> ExecuteAsync(
        RequestNeedsCleaningCommand command,
        CancellationToken cancellationToken)
    {
        var workplaceResult = WorkplaceGuard.Get(workplace);
        if (!workplaceResult.IsSuccess)
        {
            return workplaceResult.Error!;
        }

        if (command.AssignedEmployeeId == Guid.Empty)
        {
            return RoomOperationsError.AssignmentRequired();
        }

        if (!Enum.IsDefined(command.Priority))
        {
            return RoomOperationsError.InvalidPriority();
        }

        var room = await store.GetRoomAsync(command.RoomId, cancellationToken);
        if (room is null || room.PropertyId != workplace.PropertyId)
        {
            return RoomOperationsError.RoomNotFound();
        }

        if (!room.CanReceiveNeedsCleaningWork(out var roomError))
        {
            return RoomOperationsError.RoomInactive();
        }

        var openWork = await store.FindOpenWorkAsync(room.Id, cancellationToken);
        if (openWork is not null)
        {
            return RoomOperationsError.ActiveWorkAlreadyExists();
        }

        var employee = await employees.FindAssignableAsync(command.AssignedEmployeeId, cancellationToken);
        if (employee is null)
        {
            return RoomOperationsError.EmployeeNotFound();
        }

        var now = clock.UtcNow;
        var cycleId = room.ReadinessCycleId;
        if (room.CurrentReadiness != RoomReadiness.Dirty)
        {
            cycleId = Guid.CreateVersion7();
            if (!room.TryMarkDirtyForNewCycle(cycleId, out var dirtyError))
            {
                return RoomOperationsError.InvalidReadinessTransition(dirtyError ?? "The room could not become Dirty.");
            }
        }

        var work = HousekeepingWorkItem.Open(
            Guid.CreateVersion7(),
            room.Id,
            cycleId,
            employee.EmployeeId,
            command.Priority,
            now);

        store.AddWorkItem(work);
        store.AddHistory(RoomReadinessHistoryEntry.Record(
            Guid.CreateVersion7(),
            room.Id,
            cycleId,
            RoomReadiness.Dirty,
            ReadinessChangeCause.NeedsCleaning,
            now,
            command.ActorUserId,
            employee.EmployeeId,
            work.Id));

        await store.SaveChangesAsync(cancellationToken);
        return await RoomOperationsComposer.DetailAsync(store, employees, room, cancellationToken);
    }
}

public sealed record RequestNeedsCleaningCommand(
    Guid RoomId,
    Guid AssignedEmployeeId,
    TaskPriority Priority,
    Guid ActorUserId);
