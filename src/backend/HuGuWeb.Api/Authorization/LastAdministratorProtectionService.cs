namespace HuGuWeb.Api.Authorization;

public sealed class LastAdministratorProtectionService(IAuthorizationStore store)
{
    public async Task<bool> WouldRetainAdministrationAsync(
        Guid organizationId,
        Guid? deactivateMembershipId,
        Guid? deactivateRoleId,
        IReadOnlyDictionary<Guid, IReadOnlyList<string>>? replacementPermissionsByRoleId,
        Guid? stripRoleFromMembershipId,
        Guid? strippedRoleId,
        CancellationToken cancellationToken)
    {
        var remaining = await ListRemainingPermissionSetsAsync(
            organizationId,
            deactivateMembershipId,
            deactivateRoleId,
            replacementPermissionsByRoleId,
            stripRoleFromMembershipId,
            strippedRoleId,
            cancellationToken);
        return LastAdministratorGuard.OrganizationRetainsAdministration(remaining);
    }

    public async Task<IReadOnlyList<IReadOnlyList<string>>> ListRemainingPermissionSetsAsync(
        Guid organizationId,
        Guid? deactivateMembershipId,
        Guid? deactivateRoleId,
        IReadOnlyDictionary<Guid, IReadOnlyList<string>>? replacementPermissionsByRoleId,
        Guid? stripRoleFromMembershipId,
        Guid? strippedRoleId,
        CancellationToken cancellationToken)
    {
        var memberships = await store.ListMembershipsForOrganizationAsync(organizationId, cancellationToken);
        var remaining = new List<IReadOnlyList<string>>();
        foreach (var membership in memberships.Where(item => item.IsActive && item.Id != deactivateMembershipId))
        {
            var roleIds = membership.RoleAssignments
                .Select(item => item.RoleId)
                .Where(roleId =>
                    roleId != deactivateRoleId
                    && !(membership.Id == stripRoleFromMembershipId && roleId == strippedRoleId))
                .ToArray();
            var roles = new List<AuthorizationRole>();
            var permissions = new List<RolePermission>();
            foreach (var roleId in roleIds)
            {
                var role = await store.GetRoleAsync(roleId, cancellationToken);
                if (role is null)
                {
                    continue;
                }

                if (replacementPermissionsByRoleId is not null
                    && replacementPermissionsByRoleId.TryGetValue(roleId, out var replaced))
                {
                    var simulated = new AuthorizationRole
                    {
                        Id = role.Id,
                        OrganizationId = role.OrganizationId,
                        Name = role.Name,
                        Code = role.Code,
                        ScopeType = role.ScopeType,
                        IsActive = role.IsActive
                    };
                    roles.Add(simulated);
                    permissions.AddRange(replaced.Select(code => new RolePermission
                    {
                        RoleId = roleId,
                        PermissionCode = code
                    }));
                    continue;
                }

                if (!role.IsActive)
                {
                    continue;
                }

                roles.Add(role);
                permissions.AddRange(role.Permissions);
            }

            remaining.Add(EffectivePermissionCalculator.Calculate(membership, roles, permissions));
        }

        return remaining;
    }
}
