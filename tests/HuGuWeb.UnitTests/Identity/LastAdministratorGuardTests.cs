using HuGuWeb.Api.Authorization;

namespace HuGuWeb.UnitTests.Identity;

public class LastAdministratorGuardTests
{
    [Fact]
    public void OrganizationRetainsAdministration_WhenUsersAndRolesRemainAcrossMemberships()
    {
        var remaining = new IReadOnlyList<string>[]
        {
            [AuthorizationPermissions.UsersManage],
            [AuthorizationPermissions.RolesManage]
        };

        Assert.True(LastAdministratorGuard.OrganizationRetainsAdministration(remaining));
    }

    [Fact]
    public void OrganizationRetainsAdministration_FailsWhenLastUsersManageWouldLeave()
    {
        var remaining = new IReadOnlyList<string>[]
        {
            [AuthorizationPermissions.RolesManage, HrEmployeePermissions.Manage]
        };

        Assert.False(LastAdministratorGuard.OrganizationRetainsAdministration(remaining));
    }

    [Fact]
    public void Protection_DoesNotUseRoleNameAdmin()
    {
        Assert.DoesNotContain(LastAdministratorGuard.ProtectedPermissions, code =>
            code.Contains("Admin", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(
            [AuthorizationPermissions.UsersManage, AuthorizationPermissions.RolesManage],
            LastAdministratorGuard.ProtectedPermissions);
    }
}
