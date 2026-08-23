using HuGuWeb.RoomOperations.Domain;

namespace HuGuWeb.RoomOperations.Application;

public sealed class InspectRoomUseCase(
    IRoomOperationsStore store,
    IAssignableEmployeeDirectory employees,
    IRoomOperationsWorkplace workplace,
    IRoomOperationsClock clock)
{
    public async Task<RoomOperationsResult<RoomOperationsDetail>> ExecuteAsync(
        InspectRoomCommand command,
        CancellationToken cancellationToken)
    {
        var workplaceResult = WorkplaceGuard.Get(workplace);
        if (!workplaceResult.IsSuccess)
        {
            return workplaceResult.Error!;
        }

        if (command.ActorUserId == Guid.Empty)
        {
            return RoomOperationsError.InvalidRequest("inspection-not-allowed", "An inspector identity is required.");
        }

        var room = await store.GetRoomAsync(command.RoomId, cancellationToken);
        if (room is null || room.PropertyId != workplace.PropertyId)
        {
            return RoomOperationsError.RoomNotFound();
        }

        if (room.CurrentReadiness != RoomReadiness.Clean)
        {
            return RoomOperationsError.InspectionNotAllowed();
        }

        var now = clock.UtcNow;
        var completedWork = (await store.ListWorkItemsAsync(room.Id, cancellationToken))
            .Where(item => item.ReadinessCycleId == room.ReadinessCycleId)
            .OrderByDescending(item => item.CompletedAt ?? item.CreatedAt)
            .FirstOrDefault();

        if (command.Accepted)
        {
            if (!RoomInspection.TryAccept(
                    Guid.CreateVersion7(),
                    room.Id,
                    room.ReadinessCycleId,
                    command.ActorUserId,
                    now,
                    completedWork?.Id,
                    out var accepted,
                    out var acceptError)
                || accepted is null)
            {
                return RoomOperationsError.InvalidRequest("inspection-not-allowed", acceptError ?? "Inspection could not be recorded.");
            }

            if (!room.TryMarkInspected(out var inspectedError))
            {
                return RoomOperationsError.InvalidReadinessTransition(
                    inspectedError ?? "The room could not become Inspected.");
            }

            store.AddInspection(accepted);
            store.AddHistory(RoomReadinessHistoryEntry.Record(
                Guid.CreateVersion7(),
                room.Id,
                room.ReadinessCycleId,
                RoomReadiness.Inspected,
                ReadinessChangeCause.InspectionAccepted,
                now,
                command.ActorUserId,
                completedWork?.AssignedEmployeeId,
                completedWork?.Id,
                accepted.Id));

            if (!room.TryMarkReady(out var readyError))
            {
                return RoomOperationsError.InvalidReadinessTransition(
                    readyError ?? "The room could not become Ready.");
            }

            store.AddHistory(RoomReadinessHistoryEntry.Record(
                Guid.CreateVersion7(),
                room.Id,
                room.ReadinessCycleId,
                RoomReadiness.Ready,
                ReadinessChangeCause.InspectionAccepted,
                now,
                command.ActorUserId,
                completedWork?.AssignedEmployeeId,
                completedWork?.Id,
                accepted.Id));
        }
        else
        {
            if (string.IsNullOrWhiteSpace(command.Reason))
            {
                return RoomOperationsError.RejectionReasonRequired();
            }

            if (!RoomInspection.TryReject(
                    Guid.CreateVersion7(),
                    room.Id,
                    room.ReadinessCycleId,
                    command.ActorUserId,
                    command.Reason,
                    now,
                    completedWork?.Id,
                    out var rejected,
                    out var rejectError)
                || rejected is null)
            {
                return rejectError == "A rejection reason is required."
                    ? RoomOperationsError.RejectionReasonRequired()
                    : RoomOperationsError.InvalidRequest("rejection-reason-required", rejectError ?? "Inspection could not be recorded.");
            }

            var reworkCycleId = Guid.CreateVersion7();
            if (!room.TryMarkDirtyForNewCycle(reworkCycleId, out var dirtyError))
            {
                return RoomOperationsError.InvalidReadinessTransition(dirtyError ?? "The room could not return to Dirty.");
            }

            var assigneeId = completedWork?.AssignedEmployeeId;
            if (assigneeId is null)
            {
                return RoomOperationsError.AssignmentRequired();
            }

            var rework = HousekeepingWorkItem.Open(
                Guid.CreateVersion7(),
                room.Id,
                reworkCycleId,
                assigneeId.Value,
                completedWork!.Priority,
                now,
                HousekeepingWorkOrigin.Rework,
                rejected.Id);

            store.AddInspection(rejected);
            store.AddWorkItem(rework);
            store.AddHistory(RoomReadinessHistoryEntry.Record(
                Guid.CreateVersion7(),
                room.Id,
                reworkCycleId,
                RoomReadiness.Dirty,
                ReadinessChangeCause.InspectionRejected,
                now,
                command.ActorUserId,
                assigneeId,
                rework.Id,
                rejected.Id,
                rejected.Reason));
        }

        await store.SaveChangesAsync(cancellationToken);
        return await RoomOperationsComposer.DetailAsync(store, employees, room, cancellationToken);
    }
}

public sealed record InspectRoomCommand(
    Guid RoomId,
    bool Accepted,
    string? Reason,
    Guid ActorUserId);
