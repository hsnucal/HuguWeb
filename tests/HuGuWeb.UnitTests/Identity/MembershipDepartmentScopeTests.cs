using System.Reflection;
using HuGuWeb.Api.Authorization;
using HuGuWeb.Api.Identity;
using HuGuWeb.Workforce.Application;
using HuGuWeb.Workforce.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HuGuWeb.UnitTests.Identity;

public class MembershipDepartmentScopeTests
{
    [Fact]
    public async Task PropertyMembership_ZeroScopes_ReturnsNull_PropertyWide()
    {
        var membership = CreatePropertyMembership();
        var access = new MembershipDepartmentAccess(new FakeAuthorizationStore([membership]));

        var allowed = await access.GetAllowedDepartmentsAsync(membership.Id, CancellationToken.None);

        Assert.Null(allowed);
    }

    [Fact]
    public async Task PropertyMembership_OneDepartment_ReturnsThatId()
    {
        var departmentId = Guid.CreateVersion7();
        var membership = CreatePropertyMembership(departmentIds: [departmentId]);
        var access = new MembershipDepartmentAccess(new FakeAuthorizationStore([membership]));

        var allowed = await access.GetAllowedDepartmentsAsync(membership.Id, CancellationToken.None);

        Assert.NotNull(allowed);
        Assert.Equal([departmentId], allowed);
    }

    [Fact]
    public async Task PropertyMembership_MultipleDepartments_ReturnsAllIds()
    {
        var a = Guid.CreateVersion7();
        var b = Guid.CreateVersion7();
        var membership = CreatePropertyMembership(departmentIds: [a, b]);
        var access = new MembershipDepartmentAccess(new FakeAuthorizationStore([membership]));

        var allowed = await access.GetAllowedDepartmentsAsync(membership.Id, CancellationToken.None);

        Assert.NotNull(allowed);
        Assert.Equal(2, allowed.Count);
        Assert.Contains(a, allowed);
        Assert.Contains(b, allowed);
    }

    [Fact]
    public async Task OrganizationMembership_ReturnsNull_EvenIfScopesPresent()
    {
        var membership = new UserMembership
        {
            Id = Guid.CreateVersion7(),
            UserId = "user-1",
            OrganizationId = Guid.CreateVersion7(),
            PropertyId = null,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        membership.DepartmentScopes.Add(new UserMembershipDepartmentScope
        {
            Id = Guid.CreateVersion7(),
            UserMembershipId = membership.Id,
            DepartmentId = Guid.CreateVersion7(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        var access = new MembershipDepartmentAccess(new FakeAuthorizationStore([membership]));

        var allowed = await access.GetAllowedDepartmentsAsync(membership.Id, CancellationToken.None);

        Assert.Null(allowed);
    }

    [Fact]
    public async Task InactiveMembership_ReturnsNull()
    {
        var membership = CreatePropertyMembership(departmentIds: [Guid.CreateVersion7()]);
        membership.IsActive = false;
        var access = new MembershipDepartmentAccess(new FakeAuthorizationStore([membership]));

        var allowed = await access.GetAllowedDepartmentsAsync(membership.Id, CancellationToken.None);

        Assert.Null(allowed);
    }

    [Fact]
    public async Task NullMembershipId_ReturnsNull()
    {
        var access = new MembershipDepartmentAccess(new FakeAuthorizationStore([]));

        var allowed = await access.GetAllowedDepartmentsAsync(null, CancellationToken.None);

        Assert.Null(allowed);
    }

    [Fact]
    public async Task ReplaceDepartmentScopes_MissingMembership_NotFound()
    {
        var admin = CreateAdministration(new FakeAuthorizationStore([]), FakeWorkforceProxy.Create().AsStore());

        var result = await admin.ReplaceDepartmentScopesAsync(
            Guid.CreateVersion7(),
            [],
            "actor",
            null,
            null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("membership-not-found", result.Error!.Code);
    }

    [Fact]
    public async Task ReplaceDepartmentScopes_OrganizationMembership_Rejected()
    {
        var membership = new UserMembership
        {
            Id = Guid.CreateVersion7(),
            UserId = "user-1",
            OrganizationId = Guid.CreateVersion7(),
            PropertyId = null,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        var admin = CreateAdministration(
            new FakeAuthorizationStore([membership]),
            FakeWorkforceProxy.Create().AsStore());

        var result = await admin.ReplaceDepartmentScopesAsync(
            membership.Id,
            [Guid.CreateVersion7()],
            "actor",
            null,
            null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("department-scopes-require-property", result.Error!.Code);
    }

    [Fact]
    public async Task ReplaceDepartmentScopes_EmptyList_ClearsScopes_PropertyWide()
    {
        var membership = CreatePropertyMembership([Guid.CreateVersion7(), Guid.CreateVersion7()]);
        var store = new FakeAuthorizationStore([membership]);
        var admin = CreateAdministration(store, FakeWorkforceProxy.Create().AsStore());

        var result = await admin.ReplaceDepartmentScopesAsync(
            membership.Id,
            [],
            "actor",
            null,
            null,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.DepartmentScopes);
        Assert.Contains(
            store.Audits,
            item => item.Action == AuthorizationAuditActions.MembershipDepartmentScopesChanged
                && item.Details == "property-wide");
    }

    [Fact]
    public async Task ReplaceDepartmentScopes_ValidDepartments_ReplacesSet()
    {
        var orgId = Guid.CreateVersion7();
        var propertyId = Guid.CreateVersion7();
        var deptA = Guid.CreateVersion7();
        var deptB = Guid.CreateVersion7();
        var membership = CreatePropertyMembership([Guid.CreateVersion7()], orgId, propertyId);
        var store = new FakeAuthorizationStore([membership]);
        var workforce = FakeWorkforceProxy.Create();
        workforce.SeedProperty(propertyId, orgId);
        workforce.SeedDepartment(deptA, propertyId);
        workforce.SeedDepartment(deptB, propertyId);
        var admin = CreateAdministration(store, workforce.AsStore());

        var result = await admin.ReplaceDepartmentScopesAsync(
            membership.Id,
            [deptA, deptB, deptA],
            "actor",
            orgId,
            propertyId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.DepartmentScopes.Count);
        Assert.Equal(
            new HashSet<Guid> { deptA, deptB },
            result.Value.DepartmentScopes.Select(item => item.DepartmentId).ToHashSet());
    }

    [Fact]
    public async Task ReplaceDepartmentScopes_UnknownDepartment_NotFound()
    {
        var membership = CreatePropertyMembership();
        var admin = CreateAdministration(
            new FakeAuthorizationStore([membership]),
            FakeWorkforceProxy.Create().AsStore());

        var result = await admin.ReplaceDepartmentScopesAsync(
            membership.Id,
            [Guid.CreateVersion7()],
            "actor",
            null,
            null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("department-not-found", result.Error!.Code);
    }

    [Fact]
    public async Task ReplaceDepartmentScopes_DepartmentOutsideProperty_Rejected()
    {
        var orgId = Guid.CreateVersion7();
        var propertyId = Guid.CreateVersion7();
        var otherPropertyId = Guid.CreateVersion7();
        var deptId = Guid.CreateVersion7();
        var membership = CreatePropertyMembership(organizationId: orgId, propertyId: propertyId);
        var workforce = FakeWorkforceProxy.Create();
        workforce.SeedProperty(propertyId, orgId);
        workforce.SeedProperty(otherPropertyId, orgId);
        workforce.SeedDepartment(deptId, otherPropertyId);
        var admin = CreateAdministration(new FakeAuthorizationStore([membership]), workforce.AsStore());

        var result = await admin.ReplaceDepartmentScopesAsync(
            membership.Id,
            [deptId],
            "actor",
            null,
            null,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("department-not-in-membership-property", result.Error!.Code);
    }

    private static AuthorizationAdministrationService CreateAdministration(
        FakeAuthorizationStore store,
        IWorkforceStore workforce)
    {
        var userManager = new UserManager<ApplicationUser>(
            new EmptyUserStore(),
            Options.Create(new IdentityOptions()),
            new PasswordHasher<ApplicationUser>(),
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);

        return new AuthorizationAdministrationService(
            store,
            workforce,
            userManager,
            new SecurityStampRefreshService(userManager),
            new LastAdministratorProtectionService(store),
            TimeProvider.System);
    }

    private static UserMembership CreatePropertyMembership(
        Guid[]? departmentIds = null,
        Guid? organizationId = null,
        Guid? propertyId = null)
    {
        var membership = new UserMembership
        {
            Id = Guid.CreateVersion7(),
            UserId = "user-1",
            OrganizationId = organizationId ?? Guid.CreateVersion7(),
            PropertyId = propertyId ?? Guid.CreateVersion7(),
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
        foreach (var departmentId in departmentIds ?? [])
        {
            membership.DepartmentScopes.Add(new UserMembershipDepartmentScope
            {
                Id = Guid.CreateVersion7(),
                UserMembershipId = membership.Id,
                DepartmentId = departmentId,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });
        }

        return membership;
    }

    private sealed class FakeAuthorizationStore(IReadOnlyList<UserMembership> memberships) : IAuthorizationStore
    {
        private readonly List<UserMembership> _memberships = memberships.ToList();
        public List<AuthorizationAuditRecord> Audits { get; } = [];

        public Task<UserMembership?> GetMembershipAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_memberships.FirstOrDefault(item => item.Id == id));

        public void AddDepartmentScope(UserMembershipDepartmentScope scope)
        {
            var membership = _memberships.First(item => item.Id == scope.UserMembershipId);
            membership.DepartmentScopes.Add(scope);
        }

        public void RemoveDepartmentScope(UserMembershipDepartmentScope scope)
        {
            var membership = _memberships.First(item => item.Id == scope.UserMembershipId);
            membership.DepartmentScopes.Remove(scope);
        }

        public void AddAudit(AuthorizationAuditRecord record) => Audits.Add(record);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyList<UserMembership>> ListMembershipsForUserAsync(
            string userId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<UserMembership>> ListMembershipsAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<UserMembership>> ListMembershipsForOrganizationAsync(
            Guid organizationId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<UserMembership>>(
                _memberships.Where(item => item.OrganizationId == organizationId).ToArray());

        public Task<AuthorizationRole?> GetRoleAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<AuthorizationRole?>(null);

        public Task<AuthorizationRole?> FindRoleByCodeAsync(
            Guid organizationId,
            string code,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<AuthorizationRole>> ListRolesAsync(
            Guid organizationId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<RolePermission>> ListPermissionsForRolesAsync(
            IReadOnlyCollection<Guid> roleIds,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EmployeeAccountLink?> FindLinkByUserAsync(
            string userId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EmployeeAccountLink?> FindLinkByEmployeeAsync(
            Guid employeeId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<EmployeeAccountLink>> ListLinksAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<string>> ListUserIdsForRoleAsync(
            Guid roleId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public void AddMembership(UserMembership membership) => throw new NotSupportedException();
        public void AddRole(AuthorizationRole role) => throw new NotSupportedException();
        public void AddPermission(RolePermission permission) => throw new NotSupportedException();
        public void RemovePermission(RolePermission permission) => throw new NotSupportedException();
        public void AddAssignment(UserRoleAssignment assignment) => throw new NotSupportedException();
        public void RemoveAssignment(UserRoleAssignment assignment) => throw new NotSupportedException();
        public void AddLink(EmployeeAccountLink link) => throw new NotSupportedException();
    }

    // DispatchProxy subclasses must not be sealed.
    private class FakeWorkforceProxy : DispatchProxy
    {
        private readonly Dictionary<Guid, Department> _departments = new();
        private readonly Dictionary<Guid, Property> _properties = new();

        public static FakeWorkforceProxy Create() =>
            (FakeWorkforceProxy)Create<IWorkforceStore, FakeWorkforceProxy>()!;

        public IWorkforceStore AsStore() => (IWorkforceStore)this;

        public void SeedProperty(Guid propertyId, Guid organizationId) =>
            _properties[propertyId] = new Property(propertyId, organizationId, $"Property-{propertyId:N}"[..20], "UTC");

        public void SeedDepartment(Guid departmentId, Guid propertyId)
        {
            Assert.True(
                Department.TryCreate(
                    departmentId,
                    propertyId,
                    $"Dept-{departmentId:N}"[..12],
                    null,
                    out var department,
                    out var error),
                error);
            _departments[departmentId] = department!;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == nameof(IWorkforceStore.GetDepartmentAsync))
            {
                var id = (Guid)args![0]!;
                return Task.FromResult(_departments.GetValueOrDefault(id));
            }

            if (targetMethod.Name == nameof(IWorkforceStore.GetPropertyAsync))
            {
                var id = (Guid)args![0]!;
                return Task.FromResult(_properties.GetValueOrDefault(id));
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private sealed class EmptyUserStore : IUserStore<ApplicationUser>
    {
        public void Dispose()
        {
        }

        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.Id);

        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.UserName);

        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.NormalizedUserName);

        public Task SetNormalizedUserNameAsync(
            ApplicationUser user,
            string? normalizedName,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) =>
            Task.FromResult(IdentityResult.Success);

        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult<ApplicationUser?>(null);

        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
            Task.FromResult<ApplicationUser?>(null);
    }
}
