namespace HuGuWeb.Api.Authorization;

public static class LastAdministratorGuard
{
    public static readonly IReadOnlyList<string> ProtectedPermissions =
    [
        AuthorizationPermissions.UsersManage,
        AuthorizationPermissions.RolesManage
    ];

    public static bool OrganizationRetainsAdministration(
        IEnumerable<IReadOnlyList<string>> remainingEffectivePermissions)
    {
        var usersManage = false;
        var rolesManage = false;
        foreach (var permissions in remainingEffectivePermissions)
        {
            if (permissions.Contains(AuthorizationPermissions.UsersManage, StringComparer.Ordinal))
            {
                usersManage = true;
            }

            if (permissions.Contains(AuthorizationPermissions.RolesManage, StringComparer.Ordinal))
            {
                rolesManage = true;
            }

            if (usersManage && rolesManage)
            {
                return true;
            }
        }

        return false;
    }
}
