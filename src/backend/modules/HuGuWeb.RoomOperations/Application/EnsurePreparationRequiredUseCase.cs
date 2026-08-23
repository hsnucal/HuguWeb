using HuGuWeb.RoomOperations.Domain;

namespace HuGuWeb.RoomOperations.Application;

public sealed class EnsurePreparationRequiredUseCase(
    IRoomOperationsStore store,
    IRoomOperationsWorkplace workplace,
    IRoomOperationsClock clock)
{
    public async Task<RoomOperationsResult<PreparationRequiredOutcome>> ExecuteAsync(
        EnsurePreparationRequiredCommand command,
        CancellationToken cancellationToken)
    {
        var workplaceResult = WorkplaceGuard.Get(workplace);
        if (!workplaceResult.IsSuccess)
        {
            return workplaceResult.Error!;
        }

        var room = await store.GetRoomAsync(command.RoomId, cancellationToken);
        if (room is null || room.PropertyId != workplace.PropertyId)
        {
            return RoomOperationsError.RoomNotFound();
        }

        if (!room.CanReceiveNeedsCleaningWork(out _))
        {
            return RoomOperationsError.RoomInactive();
        }

        var openWork = await store.FindOpenWorkAsync(room.Id, cancellationToken);
        if (openWork is not null)
        {
            return new PreparationRequiredOutcome(room.Id, room.CurrentReadiness, ReusedExistingWork: true, ReadinessChanged: false);
        }

        if (room.CurrentReadiness == RoomReadiness.Dirty)
        {
            return new PreparationRequiredOutcome(room.Id, room.CurrentReadiness, ReusedExistingWork: false, ReadinessChanged: false);
        }

        var now = clock.UtcNow;
        var cycleId = Guid.CreateVersion7();
        if (!room.TryMarkDirtyForNewCycle(cycleId, out var dirtyError))
        {
            return RoomOperationsError.InvalidReadinessTransition(dirtyError ?? "The room could not become Dirty.");
        }

        store.AddHistory(RoomReadinessHistoryEntry.Record(
            Guid.CreateVersion7(),
            room.Id,
            cycleId,
            RoomReadiness.Dirty,
            ReadinessChangeCause.NeedsCleaning,
            now,
            command.ActorUserId,
            actorEmployeeId: null,
            workItemId: null,
            inspectionId: null,
            comment: command.Comment));

        await store.SaveChangesAsync(cancellationToken);
        return new PreparationRequiredOutcome(room.Id, room.CurrentReadiness, ReusedExistingWork: false, ReadinessChanged: true);
    }
}

public sealed record EnsurePreparationRequiredCommand(Guid RoomId, Guid ActorUserId, string? Comment = null);

public sealed record PreparationRequiredOutcome(
    Guid RoomId,
    RoomReadiness Readiness,
    bool ReusedExistingWork,
    bool ReadinessChanged);
