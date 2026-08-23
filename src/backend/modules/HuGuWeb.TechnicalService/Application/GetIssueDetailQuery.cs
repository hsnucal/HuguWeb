namespace HuGuWeb.TechnicalService.Application;

public sealed class GetIssueDetailQuery(
    ITechnicalServiceStore store,
    IAssignableEmployeeDirectory employees,
    IRoomIdentityDirectory rooms,
    ITechnicalServiceWorkplace workplace)
{
    public async Task<TechnicalServiceResult<MaintenanceIssueDetail>> ExecuteAsync(
        Guid issueId,
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

        return await TechnicalServiceComposer.DetailAsync(store, employees, rooms, issue, cancellationToken);
    }
}
