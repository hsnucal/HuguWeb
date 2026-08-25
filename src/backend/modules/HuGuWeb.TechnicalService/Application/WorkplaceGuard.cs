namespace HuGuWeb.TechnicalService.Application;

internal static class WorkplaceGuard
{
    public static TechnicalServiceResult<ITechnicalServiceWorkplace> Get(ITechnicalServiceWorkplace workplace)
    {
        if (workplace.PropertyId == Guid.Empty)
        {
            return TechnicalServiceError.PropertyContextRequired();
        }

        if (!workplace.IsConfigured)
        {
            return TechnicalServiceError.WorkplaceNotConfigured();
        }

        return TechnicalServiceResult<ITechnicalServiceWorkplace>.Success(workplace);
    }
}
