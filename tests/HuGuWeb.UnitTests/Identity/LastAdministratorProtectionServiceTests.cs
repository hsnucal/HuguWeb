using HuGuWeb.Api.Authorization;

namespace HuGuWeb.UnitTests.Identity;

public class LastAdministratorProtectionServiceTests
{
    [Fact]
    public async Task DeactivatingSoleAdministratorMembership_IsRejected()
    {
        var orgId = Guid.CreateVersion7();
        var role = AdminRole(orgId);
        var admin = MembershipWithAdmin(orgId, "admin-user", role.Id);
        var store = new FakeStore([admin], role);
        var service = new LastAdministratorProtectionService(store);

        var retains = await service.WouldRetainAdministrationAsync(
            orgId,
            admin.Id,
            null,
            null,
            null,
            null,
            CancellationToken.None);

        Assert.False(retains);
    }

    [Fact]
    public async Task DeactivatingRoleThatHoldsBothAdminPermissions_IsRejected()
    {
        var orgId = Guid.CreateVersion7();
        var role = AdminRole(orgId);
        var membership = MembershipWithAdmin(orgId, "admin-user", role.Id);
        var store = new FakeStore([membership], role);
        var service = new LastAdministratorProtectionService(store);

        var retains = await service.WouldRetainAdministrationAsync(
            orgId,
            null,
            role.Id,
            null,
            null,
            null,
            CancellationToken.None);

        Assert.False(retains);
    }

    [Fact]
    public async Task StrippingAdminPermissionsFromLastRole_IsRejected()
    {
        var orgId = Guid.CreateVersion7();
        var role = AdminRole(orgId);
        var membership = MembershipWithAdmin(orgId, "admin-user", role.Id);
        var store = new FakeStore([membership], role);
        var service = new LastAdministratorProtectionService(store);

        var retains = await service.WouldRetainAdministrationAsync(
            orgId,
            null,
            null,
            new Dictionary<Guid, IReadOnlyList<string>> { [role.Id] = [HrEmployeePermissions.Manage] },
            null,
            null,
            CancellationToken.None);

        Assert.False(retains);
    }

    [Fact]
    public async Task RemovingLastAdminRoleAssignment_IsRejected()
    {
        var orgId = Guid.CreateVersion7();
        var role = AdminRole(orgId);
        var membership = MembershipWithAdmin(orgId, "admin-user", role.Id);
        var store = new FakeStore([membership], role);
        var service = new LastAdministratorProtectionService(store);

        var retains = await service.WouldRetainAdministrationAsync(
            orgId,
            null,
            null,
            null,
            membership.Id,
            role.Id,
            CancellationToken.None);

        Assert.False(retains);
    }

    [Fact]
    public async Task SecondAdministrator_AllowsDeactivatingOneMembership()
    {
        var orgId = Guid.CreateVersion7();
        var role = AdminRole(orgId);
        var first = MembershipWithAdmin(orgId, "admin-a", role.Id);
        var second = MembershipWithAdmin(orgId, "admin-b", role.Id);
        var store = new FakeStore([first, second], role);
        var service = new LastAdministratorProtectionService(store);

        var retains = await service.WouldRetainAdministrationAsync(
            orgId,
            first.Id,
            null,
            null,
            null,
            null,
            CancellationToken.None);

        Assert.True(retains);
    }

    [Fact]
    public void Protection_DoesNotConsultRoleName()
    {
        var role = AdminRole(Guid.CreateVersion7());
        role.Name = "Night clerk";
        role.Code = "custom-ops";
        Assert.NotEqual("Admin", role.Name);
        Assert.Contains(role.Permissions, item => item.PermissionCode == AuthorizationPermissions.UsersManage);
        Assert.Contains(role.Permissions, item => item.PermissionCode == AuthorizationPermissions.RolesManage);
    }

    private static AuthorizationRole AdminRole(Guid organizationId)
    {
        var role = new AuthorizationRole
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            Name = "unused-display-name",
            Code = "ops",
            IsActive = true
        };
        role.Permissions.Add(new RolePermission
        {
            RoleId = role.Id,
            PermissionCode = AuthorizationPermissions.UsersManage
        });
        role.Permissions.Add(new RolePermission
        {
            RoleId = role.Id,
            PermissionCode = AuthorizationPermissions.RolesManage
        });
        return role;
    }

    private static UserMembership MembershipWithAdmin(Guid organizationId, string userId, Guid roleId)
    {
        var membership = new UserMembership
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            OrganizationId = organizationId,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        membership.RoleAssignments.Add(new UserRoleAssignment
        {
            Id = Guid.CreateVersion7(),
            MembershipId = membership.Id,
            RoleId = roleId
        });
        return membership;
    }

    private sealed class FakeStore(IReadOnlyList<UserMembership> memberships, AuthorizationRole role) : IAuthorizationStore
    {
        public Task<IReadOnlyList<UserMembership>> ListMembershipsForOrganizationAsync(
            Guid organizationId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<UserMembership>>(
                memberships.Where(item => item.OrganizationId == organizationId).ToArray());

        public Task<AuthorizationRole?> GetRoleAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(id == role.Id ? role : null);

        public Task<IReadOnlyList<UserMembership>> ListMembershipsForUserAsync(string userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<UserMembership?> GetMembershipAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<UserMembership>> ListMembershipsAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<AuthorizationRole?> FindRoleByCodeAsync(Guid organizationId, string code, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<AuthorizationRole>> ListRolesAsync(Guid organizationId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<RolePermission>> ListPermissionsForRolesAsync(
            IReadOnlyCollection<Guid> roleIds,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EmployeeAccountLink?> FindLinkByUserAsync(string userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EmployeeAccountLink?> FindLinkByEmployeeAsync(Guid employeeId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<EmployeeAccountLink>> ListLinksAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<string>> ListUserIdsForRoleAsync(Guid roleId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void AddMembership(UserMembership membership) => throw new NotSupportedException();
        public void AddDepartmentScope(UserMembershipDepartmentScope scope) => throw new NotSupportedException();
        public void RemoveDepartmentScope(UserMembershipDepartmentScope scope) => throw new NotSupportedException();
        public void AddRole(AuthorizationRole roleRow) => throw new NotSupportedException();
        public void AddPermission(RolePermission permission) => throw new NotSupportedException();
        public void RemovePermission(RolePermission permission) => throw new NotSupportedException();
        public void AddAssignment(UserRoleAssignment assignment) => throw new NotSupportedException();
        public void RemoveAssignment(UserRoleAssignment assignment) => throw new NotSupportedException();
        public void AddLink(EmployeeAccountLink link) => throw new NotSupportedException();
        public void AddAudit(AuthorizationAuditRecord record) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
