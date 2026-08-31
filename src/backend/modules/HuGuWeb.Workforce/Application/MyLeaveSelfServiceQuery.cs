using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

/// <summary>
/// Self-service leave catalog (active types + balances) for linked employee.
/// Does not expose administration, entitlements history, or HR record mutation.
/// </summary>
public sealed class MyLeaveSelfServiceQuery(
    IWorkforceStore store,
    IWorkplaceContext workplaceContext,
    EmployeeLeaveQuery employeeLeaveQuery)
{
    public async Task<WorkforceResult<MyLeaveSelfServiceOverview>> ExecuteAsync(
        Guid? linkedEmployeeId,
        CancellationToken cancellationToken)
    {
        if (linkedEmployeeId is null)
        {
            return WorkforceError.LeaveRequestAccountLinkRequired();
        }

        if (!workplaceContext.HasOrganization)
        {
            return WorkforceError.WorkplaceNotConfigured();
        }

        var employee = await store.GetEmployeeAsync(linkedEmployeeId.Value, cancellationToken);
        if (employee is null || employee.OrganizationId != workplaceContext.OrganizationId)
        {
            return WorkforceError.LeaveRequestAccountLinkRequired();
        }

        var employments = await store.ListEmploymentsAsync(employee.Id, cancellationToken);
        var current = CurrentEmployment.Find(employments);
        if (!current.IsSuccess)
        {
            return WorkforceError.LeaveRequestCurrentEmploymentNotFound();
        }

        var overview = await employeeLeaveQuery.BuildAsync(
            employee.OrganizationId,
            current.Value!,
            cancellationToken);
        if (!overview.IsSuccess)
        {
            return overview.Error!;
        }

        var activeTypes = overview.Value!.LeaveTypes
            .Where(item => item.IsActive)
            .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        var balances = overview.Value.Balances
            .Where(item => activeTypes.Any(type => type.Id == item.LeaveTypeId))
            .ToArray();

        return new MyLeaveSelfServiceOverview(activeTypes, balances);
    }
}

public sealed record MyLeaveSelfServiceOverview(
    IReadOnlyList<LeaveTypeDto> LeaveTypes,
    IReadOnlyList<LeaveBalanceDto> Balances);
