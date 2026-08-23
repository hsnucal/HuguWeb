using HuGuWeb.TechnicalService.Domain;

namespace HuGuWeb.TechnicalService.Application;

internal static class IssueCommandGuard
{
    public static async Task<TechnicalServiceResult<MaintenanceIssue>> LoadAsync(
        ITechnicalServiceStore store,
        ITechnicalServiceWorkplace workplace,
        Guid issueId,
        int expectedVersion,
        CancellationToken cancellationToken)
    {
        var workplaceResult = WorkplaceGuard.Get(workplace);
        if (!workplaceResult.IsSuccess)
        {
            return workplaceResult.Error!;
        }

        var issue = await store.GetIssueAsync(issueId, cancellationToken);
        if (issue is null || issue.PropertyId != workplace.PropertyId)
        {
            return TechnicalServiceError.IssueNotFound();
        }

        if (issue.Version != expectedVersion)
        {
            return TechnicalServiceError.StaleIssue();
        }

        return issue;
    }

    public static async Task<TechnicalServiceResult<MaintenanceIssueDetail>> SaveDetailAsync(
        ITechnicalServiceStore store,
        IAssignableEmployeeDirectory employees,
        IRoomIdentityDirectory rooms,
        MaintenanceIssue issue,
        CancellationToken cancellationToken)
    {
        try
        {
            await store.SaveChangesAsync(cancellationToken);
        }
        catch (IssueConcurrencyConflictException)
        {
            return TechnicalServiceError.StaleIssue();
        }

        return await TechnicalServiceComposer.DetailAsync(store, employees, rooms, issue, cancellationToken);
    }
}
