namespace HuGuWeb.RoomOperations.Application;

internal static class WorkplaceGuard
{
    public static RoomOperationsResult<IRoomOperationsWorkplace> Get(IRoomOperationsWorkplace workplace)
    {
        if (!workplace.IsConfigured || workplace.PropertyId == Guid.Empty)
        {
            return RoomOperationsError.WorkplaceNotConfigured();
        }

        return RoomOperationsResult<IRoomOperationsWorkplace>.Success(workplace);
    }
}
