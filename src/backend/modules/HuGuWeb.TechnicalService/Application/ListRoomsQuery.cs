namespace HuGuWeb.TechnicalService.Application;

public sealed class ListRoomsQuery(
    IRoomIdentityDirectory rooms,
    ITechnicalServiceWorkplace workplace)
{
    public async Task<TechnicalServiceResult<IReadOnlyList<MaintenanceRoomItem>>> ExecuteAsync(
        CancellationToken cancellationToken)
    {
        var workplaceResult = WorkplaceGuard.Get(workplace);
        if (!workplaceResult.IsSuccess)
        {
            return workplaceResult.Error!;
        }

        var known = await rooms.ListActiveAsync(workplace.PropertyId, cancellationToken);
        return known
            .OrderBy(item => item.Number, StringComparer.OrdinalIgnoreCase)
            .Select(item => new MaintenanceRoomItem(item.RoomId, item.Number))
            .ToArray();
    }
}
