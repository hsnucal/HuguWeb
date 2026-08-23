using HuGuWeb.RoomOperations.Domain;

namespace HuGuWeb.RoomOperations.Application;

internal static class RoomOperationsComposer
{
    public static async Task<RoomOperationsDetail> DetailAsync(
        IRoomOperationsStore store,
        IAssignableEmployeeDirectory employees,
        IRoomServiceabilityLookup serviceability,
        Room room,
        CancellationToken cancellationToken)
    {
        var workItems = await store.ListWorkItemsAsync(room.Id, cancellationToken);
        var history = await store.ListHistoryAsync(room.Id, cancellationToken);
        var inspections = await store.ListInspectionsAsync(room.Id, cancellationToken);
        var names = await employees.GetEmployeesAsync(CollectEmployeeIds(workItems, history), cancellationToken);
        var snapshot = await SnapshotAsync(serviceability, room.PropertyId, room.Id, cancellationToken);

        var currentWork = workItems
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefault(item => item.IsOpen)
            ?? workItems
                .Where(item => item.ReadinessCycleId == room.ReadinessCycleId)
                .OrderByDescending(item => item.CompletedAt ?? item.CreatedAt)
                .FirstOrDefault();

        return new RoomOperationsDetail(
            room.Id,
            room.Number,
            room.IsActive,
            room.CurrentReadiness,
            room.ReadinessCycleId,
            currentWork is null ? null : ToWorkSummary(currentWork, names),
            history
                .OrderByDescending(item => item.OccurredAt)
                .ThenByDescending(item => item.Id)
                .Select(item => new ReadinessHistoryItem(
                    item.Id,
                    item.Readiness,
                    item.Cause,
                    item.OccurredAt,
                    item.ActorEmployeeId,
                    DisplayName(item.ActorEmployeeId, names),
                    item.WorkItemId,
                    item.InspectionId,
                    item.Comment))
                .ToArray(),
            inspections
                .OrderByDescending(item => item.OccurredAt)
                .ThenByDescending(item => item.Id)
                .Select(item => new InspectionHistoryItem(
                    item.Id,
                    item.Result,
                    item.OccurredAt,
                    item.InspectorUserId,
                    item.Reason,
                    item.ReadinessCycleId,
                    item.WorkItemId))
                .ToArray(),
            snapshot.Serviceability,
            snapshot.HasActiveTechnicalIssue,
            snapshot.GoverningIssueId,
            snapshot.GoverningIssueDescription);
    }

    public static async Task<RoomServiceabilitySnapshot> SnapshotAsync(
        IRoomServiceabilityLookup serviceability,
        Guid propertyId,
        Guid roomId,
        CancellationToken cancellationToken)
    {
        var snapshots = await serviceability.GetForRoomsAsync(propertyId, [roomId], cancellationToken);
        return snapshots.TryGetValue(roomId, out var snapshot)
            ? snapshot
            : RoomServiceabilitySnapshot.Available(roomId);
    }

    public static string NeededAction(Room room, HousekeepingWorkItem? currentOpenWork)
    {
        if (!room.IsActive)
        {
            return "none";
        }

        if (currentOpenWork is not null && room.CurrentReadiness == RoomReadiness.Dirty)
        {
            return "complete-cleaning";
        }

        return room.CurrentReadiness switch
        {
            RoomReadiness.Dirty => "needs-cleaning",
            RoomReadiness.Clean => "inspect",
            RoomReadiness.Inspected => "none",
            RoomReadiness.Ready => "none",
            _ => "none"
        };
    }

    public static HousekeepingWorkSummary ToWorkSummary(
        HousekeepingWorkItem work,
        IReadOnlyDictionary<Guid, AssignableEmployee> names) =>
        new(
            work.Id,
            work.State,
            work.Origin,
            work.Priority,
            work.AssignedEmployeeId,
            DisplayName(work.AssignedEmployeeId, names) ?? work.AssignedEmployeeId.ToString(),
            work.CreatedAt,
            work.CompletedAt,
            work.CompletedByEmployeeId,
            work.ReadinessCycleId,
            work.SourceInspectionId);

    private static Guid[] CollectEmployeeIds(
        IEnumerable<HousekeepingWorkItem> workItems,
        IEnumerable<RoomReadinessHistoryEntry> history)
    {
        return workItems.Select(item => item.AssignedEmployeeId)
            .Concat(workItems.Select(item => item.CompletedByEmployeeId).OfType<Guid>())
            .Concat(history.Select(item => item.ActorEmployeeId).OfType<Guid>())
            .Distinct()
            .ToArray();
    }

    private static string? DisplayName(Guid? employeeId, IReadOnlyDictionary<Guid, AssignableEmployee> names)
    {
        if (employeeId is null)
        {
            return null;
        }

        return names.TryGetValue(employeeId.Value, out var employee)
            ? $"{employee.GivenName} {employee.FamilyName}".Trim()
            : null;
    }
}
