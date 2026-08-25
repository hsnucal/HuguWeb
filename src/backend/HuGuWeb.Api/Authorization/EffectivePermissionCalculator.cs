namespace HuGuWeb.Api.Authorization;

public static class EffectivePermissionCalculator
{
    public static IReadOnlyList<string> Calculate(
        UserMembership? membership,
        IEnumerable<AuthorizationRole> roles,
        IEnumerable<RolePermission> permissions)
    {
        if (membership is null || !membership.IsActive)
        {
            return [];
        }

        var activeRoleIds = roles
            .Where(role => role.IsActive)
            .Select(role => role.Id)
            .ToHashSet();

        return permissions
            .Where(permission => activeRoleIds.Contains(permission.RoleId))
            .Select(permission => permission.PermissionCode)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();
    }

    public static UserMembership? SelectActiveMembership(
        IReadOnlyList<UserMembership> memberships,
        Guid? selectedPropertyId)
    {
        var active = memberships.Where(item => item.IsActive).ToArray();
        if (active.Length == 0)
        {
            return null;
        }

        if (selectedPropertyId is Guid propertyId)
        {
            var matchingProperty = active.FirstOrDefault(item => item.PropertyId == propertyId);
            if (matchingProperty is not null)
            {
                return matchingProperty;
            }

            var organizationWide = active.FirstOrDefault(item => item.PropertyId is null);
            if (organizationWide is not null)
            {
                return organizationWide;
            }
        }

        if (active.Length == 1)
        {
            return active[0];
        }

        var onlyOrganizationWide = active.Where(item => item.PropertyId is null).ToArray();
        var propertyMemberships = active.Where(item => item.PropertyId is not null).ToArray();
        if (propertyMemberships.Length == 0 && onlyOrganizationWide.Length == 1)
        {
            return onlyOrganizationWide[0];
        }

        if (propertyMemberships.Length == 1 && onlyOrganizationWide.Length == 0)
        {
            return propertyMemberships[0];
        }

        if (onlyOrganizationWide.Length == 1)
        {
            return onlyOrganizationWide[0];
        }

        return null;
    }

    public static Guid? ResolveOperationalPropertyId(
        UserMembership? membership,
        Guid? selectedPropertyId)
    {
        if (membership is null)
        {
            return null;
        }

        if (membership.PropertyId is Guid membershipProperty)
        {
            return membershipProperty;
        }

        return selectedPropertyId;
    }
}
