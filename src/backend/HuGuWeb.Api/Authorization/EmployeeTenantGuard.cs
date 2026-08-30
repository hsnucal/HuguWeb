using System.Security.Claims;
using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;

namespace HuGuWeb.Api.Authorization;

public sealed class EmployeeTenantGuard(IWorkforceStore store, IWorkforceClock clock)
{
    public bool IsOrganizationWide(ClaimsPrincipal user) =>
        string.Equals(
            user.FindFirstValue(AuthorizationClaims.ScopeType),
            nameof(AuthorizationScopeType.Organization),
            StringComparison.Ordinal);

    public Guid? ScopedPropertyId(ClaimsPrincipal user)
    {
        if (IsOrganizationWide(user))
        {
            return null;
        }

        return Guid.TryParse(user.FindFirstValue(AuthorizationClaims.PropertyId), out var propertyId)
            ? propertyId
            : null;
    }

    public async Task<bool> AllowsEmployeeAsync(
        ClaimsPrincipal user,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var employee = await store.GetEmployeeAsync(employeeId, cancellationToken);
        if (employee is null)
        {
            return false;
        }

        if (!Guid.TryParse(user.FindFirstValue(AuthorizationClaims.OrganizationId), out var organizationId)
            || employee.OrganizationId != organizationId)
        {
            return false;
        }

        var propertyId = ScopedPropertyId(user);
        if (propertyId is null)
        {
            return true;
        }

        return await IsAssignedToPropertyAsync(employeeId, propertyId.Value, cancellationToken);
    }

    /// <summary>
    /// Organization membership only. Property-scoped schedule access is enforced per schedule date
    /// via effective Assignment → Property (no current-property fallback).
    /// </summary>
    public async Task<bool> AllowsEmployeeInOrganizationAsync(
        ClaimsPrincipal user,
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var employee = await store.GetEmployeeAsync(employeeId, cancellationToken);
        if (employee is null)
        {
            return false;
        }

        return Guid.TryParse(user.FindFirstValue(AuthorizationClaims.OrganizationId), out var organizationId)
            && employee.OrganizationId == organizationId;
    }

    public async Task<bool> IsAssignedToPropertyAsync(
        Guid employeeId,
        Guid propertyId,
        CancellationToken cancellationToken)
    {
        var employments = await store.ListEmploymentsAsync(employeeId, cancellationToken);
        if (employments.Count == 0)
        {
            return false;
        }

        var today = clock.Today;
        var latest = employments.OrderByDescending(item => item.StartDate).First();
        var coveringDate = latest.EffectiveStatus(today) == EmploymentStatus.Ended
            ? latest.EndDate ?? latest.StartDate
            : today;
        var assignments = await store.ListAssignmentsAsync(latest.Id, cancellationToken);
        var assignment = PrimaryAssignments.Covering(assignments, coveringDate)
            ?? PrimaryAssignments.OrderedPrimaries(assignments).LastOrDefault();
        if (assignment is null)
        {
            return false;
        }

        var department = await store.GetDepartmentAsync(assignment.DepartmentId, cancellationToken);
        return department is not null && department.PropertyId == propertyId;
    }
}
