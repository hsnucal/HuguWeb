namespace HuGuWeb.TechnicalService.Application;

public sealed class ListCategoriesQuery(
    ITechnicalServiceStore store,
    ITechnicalServiceWorkplace workplace)
{
    public async Task<TechnicalServiceResult<IReadOnlyList<MaintenanceCategoryItem>>> ExecuteAsync(
        CancellationToken cancellationToken)
    {
        var workplaceResult = WorkplaceGuard.Get(workplace);
        if (!workplaceResult.IsSuccess)
        {
            return workplaceResult.Error!;
        }

        var categories = await store.ListCategoriesAsync(workplace.PropertyId, cancellationToken);
        return categories
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(item => new MaintenanceCategoryItem(item.Id, item.Name))
            .ToArray();
    }
}
