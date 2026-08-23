namespace HuGuWeb.RoomOperations.Application;

public sealed class GetRoomOperationsDetailQuery(
    IRoomOperationsStore store,
    IAssignableEmployeeDirectory employees,
    IRoomOperationsWorkplace workplace)
{
    public async Task<RoomOperationsResult<RoomOperationsDetail>> ExecuteAsync(
        Guid roomId,
        CancellationToken cancellationToken)
    {
        var workplaceResult = WorkplaceGuard.Get(workplace);
        if (!workplaceResult.IsSuccess)
        {
            return workplaceResult.Error!;
        }

        var room = await store.GetRoomAsync(roomId, cancellationToken);
        if (room is null || room.PropertyId != workplace.PropertyId)
        {
            return RoomOperationsError.RoomNotFound();
        }

        return await RoomOperationsComposer.DetailAsync(store, employees, room, cancellationToken);
    }
}
