using HuGuWeb.Api.Authorization;

namespace HuGuWeb.UnitTests.Identity;

public class EffectivePermissionCalculatorTests
{
    [Fact]
    public void InactiveMembership_GrantsNothing()
    {
        var membership = Membership(isActive: false);
        var role = Role(SystemRoleTemplates.HrManagerId, true);
        var permissions = new[]
        {
            new RolePermission { RoleId = role.Id, PermissionCode = HrEmployeePermissions.Manage }
        };

        var effective = EffectivePermissionCalculator.Calculate(membership, [role], permissions);

        Assert.Empty(effective);
    }

    [Fact]
    public void InactiveRole_IsIgnored()
    {
        var membership = Membership(isActive: true);
        var role = Role(SystemRoleTemplates.HrManagerId, isActive: false);
        var permissions = new[]
        {
            new RolePermission { RoleId = role.Id, PermissionCode = HrEmployeePermissions.Manage }
        };

        var effective = EffectivePermissionCalculator.Calculate(membership, [role], permissions);

        Assert.Empty(effective);
    }

    [Fact]
    public void MultipleRoles_UnionPermissions()
    {
        var membership = Membership(isActive: true);
        var hr = Role(SystemRoleTemplates.HrManagerId, true);
        var rooms = Role(SystemRoleTemplates.RoomOperationsManagerId, true);
        var permissions = new[]
        {
            new RolePermission { RoleId = hr.Id, PermissionCode = HrEmployeePermissions.Read },
            new RolePermission { RoleId = hr.Id, PermissionCode = WorkforcePermissions.Read },
            new RolePermission { RoleId = rooms.Id, PermissionCode = RoomOperationsPermissions.Read }
        };

        var effective = EffectivePermissionCalculator.Calculate(membership, [hr, rooms], permissions);

        Assert.Equal(
            [HrEmployeePermissions.Read, RoomOperationsPermissions.Read, WorkforcePermissions.Read],
            effective);
    }

    [Fact]
    public void RoleName_IsNotConsulted()
    {
        var membership = Membership(isActive: true);
        var role = new AuthorizationRole
        {
            Id = Guid.CreateVersion7(),
            Name = "HR Manager",
            Code = "custom-night-hr",
            IsActive = true
        };

        var effective = EffectivePermissionCalculator.Calculate(membership, [role], []);

        Assert.Empty(effective);
        Assert.Equal("HR Manager", role.Name);
    }

    [Fact]
    public void SelectActiveMembership_UsesSelectedProperty_NotOldestFallback()
    {
        var only = Membership(isActive: true);
        Assert.Same(only, EffectivePermissionCalculator.SelectActiveMembership([only], Guid.CreateVersion7()));

        var hotelX = new UserMembership
        {
            Id = Guid.CreateVersion7(),
            IsActive = true,
            PropertyId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-2)
        };
        var hotelY = new UserMembership
        {
            Id = Guid.CreateVersion7(),
            IsActive = true,
            PropertyId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var selected = EffectivePermissionCalculator.SelectActiveMembership(
            [hotelY, hotelX],
            hotelX.PropertyId);
        Assert.Equal(hotelX.Id, selected!.Id);

        Assert.Null(EffectivePermissionCalculator.SelectActiveMembership([hotelY, hotelX], selectedPropertyId: null));
    }

    [Fact]
    public void OrganizationWideMembership_DoesNotInferOperationalProperty()
    {
        var orgWide = new UserMembership
        {
            Id = Guid.CreateVersion7(),
            IsActive = true,
            OrganizationId = Guid.CreateVersion7(),
            PropertyId = null,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var selected = EffectivePermissionCalculator.SelectActiveMembership([orgWide], selectedPropertyId: null);
        Assert.Same(orgWide, selected);
        Assert.Null(EffectivePermissionCalculator.ResolveOperationalPropertyId(orgWide, selectedPropertyId: null));

        var propertyId = Guid.CreateVersion7();
        Assert.Equal(propertyId, EffectivePermissionCalculator.ResolveOperationalPropertyId(orgWide, propertyId));
    }

    [Fact]
    public void PermissionCatalog_DoesNotTreatPositionOrDepartmentAsPermissions()
    {
        Assert.DoesNotContain(PermissionCatalog.All, code => code.Contains("Kat", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(PermissionCatalog.All, code => code.Contains("department", StringComparison.OrdinalIgnoreCase));
        Assert.False(PermissionCatalog.IsKnown("HR Manager"));
        Assert.True(PermissionCatalog.IsKnown(HrEmployeePermissions.Manage));
    }

    private static UserMembership Membership(bool isActive) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            UserId = "user-1",
            OrganizationId = Guid.CreateVersion7(),
            PropertyId = Guid.CreateVersion7(),
            IsActive = isActive,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

    private static AuthorizationRole Role(Guid id, bool isActive) =>
        new()
        {
            Id = id,
            Name = "unused",
            Code = "unused",
            IsActive = isActive
        };
}
