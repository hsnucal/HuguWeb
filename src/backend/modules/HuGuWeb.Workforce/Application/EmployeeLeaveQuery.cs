using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class EmployeeLeaveQuery(IWorkforceStore store, IWorkforceClock clock, IWorkplaceContext workplaceContext)
{
    public async Task<WorkforceResult<EmployeeLeaveOverview>> ExecuteAsync(
        Guid employeeId,
        Guid? employmentId,
        CancellationToken cancellationToken)
    {
        var context = await LeaveEmploymentContext.ResolveAsync(
            store,
            workplaceContext,
            employeeId,
            employmentId,
            cancellationToken);
        if (!context.IsSuccess)
        {
            return context.Error!;
        }

        return await BuildAsync(context.Value.OrganizationId, context.Value.Employment, cancellationToken);
    }

    internal async Task<WorkforceResult<EmployeeLeaveOverview>> BuildAsync(
        Guid organizationId,
        Employment employment,
        CancellationToken cancellationToken)
    {
        var types = await store.ListLeaveTypesAsync(organizationId, cancellationToken);
        var entitlements = await store.ListLeaveEntitlementsAsync(employment.Id, cancellationToken);
        var records = await store.ListLeaveRecordsAsync(employment.Id, cancellationToken);

        var netByType = entitlements
            .GroupBy(item => item.LeaveTypeId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Amount));
        var usedByType = records
            .Where(item => item.Status == LeaveRecordStatus.Recorded)
            .GroupBy(item => item.LeaveTypeId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Amount));

        var balances = new List<LeaveBalanceDto>();
        foreach (var type in types.Where(item => item.TracksBalance))
        {
            var net = netByType.GetValueOrDefault(type.Id);
            var used = usedByType.GetValueOrDefault(type.Id);
            var referenced = netByType.ContainsKey(type.Id) || usedByType.ContainsKey(type.Id);
            if (!type.IsActive && !referenced)
            {
                continue;
            }

            balances.Add(new LeaveBalanceDto(type.Id, type.Code, type.Name, type.SystemKind, net, used, net - used));
        }

        var overview = new EmployeeLeaveOverview(
            employment.EmployeeId,
            employment.Id,
            employment.StartDate,
            employment.EndDate,
            employment.EffectiveStatus(clock.Today),
            types.Select(LeaveTypeDto.From).ToArray(),
            balances
                .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToArray(),
            entitlements.Select(LeaveEntitlementDto.From).ToArray(),
            records.Select(LeaveRecordDto.From).ToArray());
        return overview;
    }
}

public sealed record EmployeeLeaveOverview(
    Guid EmployeeId,
    Guid EmploymentId,
    DateOnly EmploymentStartDate,
    DateOnly? EmploymentEndDate,
    EmploymentStatus EmploymentStatus,
    IReadOnlyList<LeaveTypeDto> LeaveTypes,
    IReadOnlyList<LeaveBalanceDto> Balances,
    IReadOnlyList<LeaveEntitlementDto> Entitlements,
    IReadOnlyList<LeaveRecordDto> Records);

public sealed record LeaveBalanceDto(
    Guid LeaveTypeId,
    string Code,
    string Name,
    LeaveTypeSystemKind? SystemKind,
    decimal NetMovement,
    decimal Used,
    decimal Remaining);

public sealed record LeaveEntitlementDto(
    Guid Id,
    Guid LeaveTypeId,
    DateOnly EffectiveDate,
    decimal Amount,
    LeaveEntitlementSource Source,
    string? Note,
    DateTimeOffset CreatedAtUtc)
{
    public static LeaveEntitlementDto From(LeaveEntitlement entitlement) =>
        new(
            entitlement.Id,
            entitlement.LeaveTypeId,
            entitlement.EffectiveDate,
            entitlement.Amount,
            entitlement.Source,
            entitlement.Note,
            entitlement.CreatedAtUtc);
}

public sealed record LeaveRecordDto(
    Guid Id,
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Amount,
    LeaveRecordStatus Status,
    string? Note,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    string? CancellationReason)
{
    public static LeaveRecordDto From(LeaveRecord record) =>
        new(
            record.Id,
            record.LeaveTypeId,
            record.StartDate,
            record.EndDate,
            record.Amount,
            record.Status,
            record.Note,
            record.CreatedAtUtc,
            record.CancelledAtUtc,
            record.CancellationReason);
}

internal readonly record struct LeaveEmploymentResolved(Guid OrganizationId, Employment Employment);

internal static class LeaveEmploymentContext
{
    public static async Task<WorkforceResult<LeaveEmploymentResolved>> ResolveAsync(
        IWorkforceStore store,
        IWorkplaceContext workplaceContext,
        Guid employeeId,
        Guid? employmentId,
        CancellationToken cancellationToken)
    {
        if (!workplaceContext.HasOrganization)
        {
            return WorkforceError.WorkplaceNotConfigured();
        }

        var employee = await store.GetEmployeeAsync(employeeId, cancellationToken);
        if (employee is null || employee.OrganizationId != workplaceContext.OrganizationId)
        {
            return WorkforceError.EmployeeNotFound();
        }

        var employments = await store.ListEmploymentsAsync(employeeId, cancellationToken);
        if (employments.Count == 0)
        {
            return WorkforceError.EmploymentNotFound();
        }

        Employment? employment;
        if (employmentId is { } requested)
        {
            employment = employments.FirstOrDefault(item => item.Id == requested);
            if (employment is null)
            {
                return WorkforceError.EmploymentNotFound();
            }
        }
        else
        {
            employment = CurrentEmployment.TryFind(employments)
                ?? employments.OrderByDescending(item => item.StartDate).First();
        }

        return new LeaveEmploymentResolved(employee.OrganizationId, employment);
    }
}
