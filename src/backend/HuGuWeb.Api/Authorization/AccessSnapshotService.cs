using HuGuWeb.Api.Identity;
using Microsoft.AspNetCore.Identity;

namespace HuGuWeb.Api.Authorization;

public sealed class AccessSnapshotService(IAuthorizationStore store)
{
    public async Task<AccessSnapshot> GetSnapshotAsync(
        string userId,
        Guid? selectedPropertyId,
        CancellationToken cancellationToken)
    {
        var memberships = await store.ListMembershipsForUserAsync(userId, cancellationToken);
        var membership = EffectivePermissionCalculator.SelectActiveMembership(memberships, selectedPropertyId);
        if (membership is null)
        {
            var linkOnly = await store.FindLinkByUserAsync(userId, cancellationToken);
            return new AccessSnapshot(userId, null, null, null, null, null, linkOnly?.EmployeeId, []);
        }

        var roleIds = membership.RoleAssignments.Select(item => item.RoleId).ToArray();
        var roles = new List<AuthorizationRole>();
        foreach (var roleId in roleIds)
        {
            var role = await store.GetRoleAsync(roleId, cancellationToken);
            if (role is not null)
            {
                roles.Add(role);
            }
        }

        var permissions = await store.ListPermissionsForRolesAsync(roleIds, cancellationToken);
        var effective = EffectivePermissionCalculator.Calculate(membership, roles, permissions);
        var link = await store.FindLinkByUserAsync(userId, cancellationToken);
        var operationalProperty = EffectivePermissionCalculator.ResolveOperationalPropertyId(
            membership,
            selectedPropertyId);
        return new AccessSnapshot(
            userId,
            membership.Id,
            membership.OrganizationId,
            operationalProperty,
            membership.ScopeType,
            membership.PropertyId,
            link?.EmployeeId,
            effective);
    }
}

public sealed class SecurityStampRefreshService(UserManager<ApplicationUser> userManager)
{
    public async Task RefreshAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is not null)
        {
            await userManager.UpdateSecurityStampAsync(user);
        }
    }

    public async Task RefreshManyAsync(IEnumerable<string> userIds)
    {
        foreach (var userId in userIds.Distinct(StringComparer.Ordinal))
        {
            await RefreshAsync(userId);
        }
    }
}
