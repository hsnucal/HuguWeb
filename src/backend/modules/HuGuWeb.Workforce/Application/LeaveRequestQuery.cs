using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Workforce.Application;

public sealed class LeaveRequestQuery(
    IWorkforceStore store,
    IWorkplaceContext workplaceContext,
    LeaveRequestComposer composer)
{
    public async Task<WorkforceResult<LeaveRequestListPageDto>> ListMineAsync(
        Guid linkedEmployeeId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var employee = await RequireOrgEmployeeAsync(linkedEmployeeId, cancellationToken);
        if (!employee.IsSuccess)
        {
            return employee.Error!;
        }

        var requests = await store.ListLeaveRequestsForEmployeeAsync(linkedEmployeeId, cancellationToken);
        var ordered = requests
            .OrderByDescending(item => item.CreatedAtUtc)
            .ThenByDescending(item => item.Id)
            .ToArray();
        var total = ordered.Length;
        var slice = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
        var items = new List<LeaveRequestListItemDto>(slice.Length);
        foreach (var request in slice)
        {
            var item = await composer.ComposeListItemAsync(request, cancellationToken);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return new LeaveRequestListPageDto(items, page, pageSize, total);
    }

    public async Task<WorkforceResult<LeaveRequestDetailDto>> GetMineAsync(
        Guid linkedEmployeeId,
        Guid leaveRequestId,
        CancellationToken cancellationToken)
    {
        var employee = await RequireOrgEmployeeAsync(linkedEmployeeId, cancellationToken);
        if (!employee.IsSuccess)
        {
            return employee.Error!;
        }

        var request = await store.GetLeaveRequestAsync(leaveRequestId, cancellationToken);
        if (request is null)
        {
            return WorkforceError.LeaveRequestNotOwned();
        }

        var employment = await store.GetEmploymentAsync(request.EmploymentId, cancellationToken);
        if (employment is null || employment.EmployeeId != linkedEmployeeId)
        {
            return WorkforceError.LeaveRequestNotOwned();
        }

        var detail = await composer.ComposeDetailAsync(request, cancellationToken);
        return detail is null ? WorkforceError.LeaveRequestNotFound() : detail;
    }

    public async Task<WorkforceResult<LeaveRequestListPageDto>> ListManagedAsync(
        LeaveRequestListFilter filter,
        CancellationToken cancellationToken)
    {
        if (!workplaceContext.HasOrganization)
        {
            return WorkforceError.WorkplaceNotConfigured();
        }

        var candidates = await store.ListAllLeaveRequestsAsync(cancellationToken);
        var matched = new List<(LeaveRequest Request, Employee Employee, Department Department, LeaveType LeaveType)>();

        foreach (var request in candidates)
        {
            var employment = await store.GetEmploymentAsync(request.EmploymentId, cancellationToken);
            if (employment is null)
            {
                continue;
            }

            var employee = await store.GetEmployeeAsync(employment.EmployeeId, cancellationToken);
            if (employee is null || employee.OrganizationId != workplaceContext.OrganizationId)
            {
                continue;
            }

            var assignment = await store.GetAssignmentAsync(request.AssignmentId, cancellationToken);
            var department = assignment is null
                ? null
                : await store.GetDepartmentAsync(assignment.DepartmentId, cancellationToken);
            var leaveType = await store.GetLeaveTypeAsync(request.LeaveTypeId, cancellationToken);
            if (assignment is null || department is null || leaveType is null)
            {
                continue;
            }

            if (filter.ScopedPropertyId is { } propertyId && department.PropertyId != propertyId)
            {
                continue;
            }

            if (!LeaveRequestWorkplaceAccess.Allows(
                    filter.ScopedPropertyId,
                    filter.AllowedDepartmentIds,
                    department.PropertyId,
                    department.Id))
            {
                continue;
            }

            if (filter.DepartmentId is { } deptFilter && department.Id != deptFilter)
            {
                continue;
            }

            if (filter.Status is { } status && request.Status != status)
            {
                continue;
            }

            if (filter.ApprovalStage is { } stage && request.ApprovalStage != stage)
            {
                continue;
            }

            if (filter.LeaveTypeId is { } typeId && request.LeaveTypeId != typeId)
            {
                continue;
            }

            if (filter.From is { } from && request.EndDate < from)
            {
                continue;
            }

            if (filter.To is { } to && request.StartDate > to)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(filter.EmployeeSearch))
            {
                var term = filter.EmployeeSearch.Trim();
                var haystack =
                    $"{employee.PersonnelNumber} {employee.GivenName} {employee.FamilyName}";
                if (haystack.Contains(term, StringComparison.CurrentCultureIgnoreCase) is false)
                {
                    continue;
                }
            }

            matched.Add((request, employee, department, leaveType));
        }

        var ordered = matched
            .OrderByDescending(item => item.Request.CreatedAtUtc)
            .ThenByDescending(item => item.Request.Id)
            .ToArray();
        var total = ordered.Length;
        var pageItems = ordered.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToArray();
        var items = new List<LeaveRequestListItemDto>(pageItems.Length);
        foreach (var row in pageItems)
        {
            var item = await composer.ComposeListItemAsync(row.Request, cancellationToken);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return new LeaveRequestListPageDto(items, filter.Page, filter.PageSize, total);
    }

    public async Task<WorkforceResult<LeaveRequestDetailDto>> GetManagedAsync(
        Guid leaveRequestId,
        Guid? scopedPropertyId,
        IReadOnlySet<Guid>? allowedDepartmentIds,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessibleRequestAsync(
            leaveRequestId,
            scopedPropertyId,
            allowedDepartmentIds,
            cancellationToken);
        if (!access.IsSuccess)
        {
            return access.Error!;
        }

        var detail = await composer.ComposeDetailAsync(access.Value, cancellationToken);
        return detail is null ? WorkforceError.LeaveRequestNotFound() : detail;
    }

    public async Task<WorkforceResult<LeaveRequest>> ResolveAccessibleRequestAsync(
        Guid leaveRequestId,
        Guid? scopedPropertyId,
        IReadOnlySet<Guid>? allowedDepartmentIds,
        CancellationToken cancellationToken)
    {
        if (!workplaceContext.HasOrganization)
        {
            return WorkforceError.WorkplaceNotConfigured();
        }

        var request = await store.GetLeaveRequestAsync(leaveRequestId, cancellationToken);
        if (request is null)
        {
            return WorkforceError.LeaveRequestNotFound();
        }

        var employment = await store.GetEmploymentAsync(request.EmploymentId, cancellationToken);
        var employee = employment is null
            ? null
            : await store.GetEmployeeAsync(employment.EmployeeId, cancellationToken);
        if (employee is null || employee.OrganizationId != workplaceContext.OrganizationId)
        {
            return WorkforceError.LeaveRequestNotFound();
        }

        var assignment = await store.GetAssignmentAsync(request.AssignmentId, cancellationToken);
        var department = assignment is null
            ? null
            : await store.GetDepartmentAsync(assignment.DepartmentId, cancellationToken);
        if (assignment is null || department is null)
        {
            return WorkforceError.LeaveRequestNotFound();
        }

        if (!LeaveRequestWorkplaceAccess.Allows(
                scopedPropertyId,
                allowedDepartmentIds,
                department.PropertyId,
                department.Id))
        {
            return WorkforceError.LeaveRequestDepartmentAccessDenied();
        }

        return request;
    }

    private async Task<WorkforceResult<Employee>> RequireOrgEmployeeAsync(
        Guid employeeId,
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

        return employee;
    }
}

public sealed record LeaveRequestListFilter(
    Guid? ScopedPropertyId,
    IReadOnlySet<Guid>? AllowedDepartmentIds,
    LeaveRequestStatus? Status,
    LeaveRequestApprovalStage? ApprovalStage,
    Guid? LeaveTypeId,
    Guid? DepartmentId,
    DateOnly? From,
    DateOnly? To,
    string? EmployeeSearch,
    int Page,
    int PageSize);
