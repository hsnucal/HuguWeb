namespace HuGuWeb.RoomOperations.Application;

internal static class WorkplaceGuard
{
    public static RoomOperationsResult<IRoomOperationsWorkplace> Get(IRoomOperationsWorkplace workplace)
    {
        if (workplace.PropertyId == Guid.Empty)
        {
            return RoomOperationsError.PropertyContextRequired();
        }

        if (!workplace.IsConfigured)
        {
            return RoomOperationsError.WorkplaceNotConfigured();
        }

        return RoomOperationsResult<IRoomOperationsWorkplace>.Success(workplace);
    }
}
