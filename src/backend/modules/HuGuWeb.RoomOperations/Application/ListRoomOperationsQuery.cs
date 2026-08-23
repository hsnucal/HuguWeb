using HuGuWeb.RoomOperations.Domain;

namespace HuGuWeb.RoomOperations.Application;

public sealed class ListRoomOperationsQuery(
    IRoomOperationsStore store,
    IAssignableEmployeeDirectory employees,
    IRoomOperationsWorkplace workplace)
{
    public async Task<RoomOperationsResult<IReadOnlyList<RoomOperationsListItem>>> ExecuteAsync(
        CancellationToken cancellationToken)
    {
        var workplaceResult = WorkplaceGuard.Get(workplace);
        if (!workplaceResult.IsSuccess)
        {
            return workplaceResult.Error!;
        }

        var rooms = await store.ListRoomsAsync(workplace.PropertyId, cancellationToken);
        var workItems = await store.ListWorkItemsForRoomsAsync(rooms.Select(item => item.Id).ToArray(), cancellationToken);
        var names = await employees.GetEmployeesAsync(
            workItems.Select(item => item.AssignedEmployeeId).Distinct().ToArray(),
            cancellationToken);
        var workByRoom = workItems.ToLookup(item => item.RoomId);

        var items = rooms
            .OrderBy(room => room.Number, StringComparer.OrdinalIgnoreCase)
            .Select(room =>
            {
                var currentOpen = workByRoom[room.Id].FirstOrDefault(item => item.IsOpen);
                var current = currentOpen
                    ?? workByRoom[room.Id]
                        .Where(item => item.ReadinessCycleId == room.ReadinessCycleId)
                        .OrderByDescending(item => item.CompletedAt ?? item.CreatedAt)
                        .FirstOrDefault();
                var display = current is null ? null : RoomOperationsComposer.ToWorkSummary(current, names);
                return new RoomOperationsListItem(
                    room.Id,
                    room.Number,
                    room.IsActive,
                    room.CurrentReadiness,
                    room.ReadinessCycleId,
                    display?.Id,
                    display?.State,
                    display?.Origin,
                    display?.Priority,
                    display?.AssignedEmployeeId,
                    display?.AssignedEmployeeName,
                    RoomOperationsComposer.NeededAction(room, currentOpen));
            })
            .ToArray();

        return items;
    }
}
