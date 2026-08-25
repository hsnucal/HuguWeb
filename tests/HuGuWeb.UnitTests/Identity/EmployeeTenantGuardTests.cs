using System.Security.Claims;
using HuGuWeb.Api.Authorization;
using HuGuWeb.UnitTests.Workforce;

namespace HuGuWeb.UnitTests.Identity;

public class EmployeeTenantGuardTests
{
    [Fact]
    public async Task PropertyScopedMembership_AllowsEmployeeAtThatProperty_DeniesOtherProperty()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);
        Assert.True(hired.IsSuccess);
        var employeeId = hired.Value!.EmployeeId;
        var guard = new EmployeeTenantGuard(harness.Store, harness.Clock);

        var hotelX = Principal(harness.OrganizationId, harness.PropertyId, AuthorizationScopeType.Property);
        var hotelY = Principal(harness.OrganizationId, harness.OtherPropertyId, AuthorizationScopeType.Property);

        Assert.True(await guard.AllowsEmployeeAsync(hotelX, employeeId, CancellationToken.None));
        Assert.False(await guard.AllowsEmployeeAsync(hotelY, employeeId, CancellationToken.None));
    }

    [Fact]
    public async Task OrganizationScopedMembership_AllowsEmployeeInOrganization()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);
        var guard = new EmployeeTenantGuard(harness.Store, harness.Clock);
        var corporate = Principal(harness.OrganizationId, harness.PropertyId, AuthorizationScopeType.Organization);

        Assert.True(await guard.AllowsEmployeeAsync(corporate, hired.Value!.EmployeeId, CancellationToken.None));
    }

    [Fact]
    public async Task OtherOrganization_IsDenied()
    {
        var harness = new WorkforceHarness();
        var hired = await harness.Hire.ExecuteAsync(harness.HireCommand(), CancellationToken.None);
        var guard = new EmployeeTenantGuard(harness.Store, harness.Clock);
        var otherOrg = Principal(Guid.CreateVersion7(), harness.PropertyId, AuthorizationScopeType.Property);

        Assert.False(await guard.AllowsEmployeeAsync(otherOrg, hired.Value!.EmployeeId, CancellationToken.None));
    }

    private static ClaimsPrincipal Principal(
        Guid organizationId,
        Guid propertyId,
        AuthorizationScopeType scope)
    {
        var identity = new ClaimsIdentity("Test");
        identity.AddClaim(new Claim(AuthorizationClaims.OrganizationId, organizationId.ToString()));
        identity.AddClaim(new Claim(AuthorizationClaims.PropertyId, propertyId.ToString()));
        identity.AddClaim(new Claim(AuthorizationClaims.ScopeType, scope.ToString()));
        return new ClaimsPrincipal(identity);
    }
}
