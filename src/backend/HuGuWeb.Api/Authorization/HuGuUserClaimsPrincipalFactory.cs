using System.Security.Claims;
using HuGuWeb.Api.Context;
using HuGuWeb.Api.Identity;
using HuGuWeb.Workforce.Application;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace HuGuWeb.Api.Authorization;

public sealed class HuGuUserClaimsPrincipalFactory(
    IAuthorizationStore authorizationStore,
    IHttpContextAccessor httpContextAccessor,
    IOptions<IdentityOptions> identityOptions)
    : IUserClaimsPrincipalFactory<ApplicationUser>
{
    public async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var identity = new ClaimsIdentity(
            IdentityConstants.ApplicationScheme,
            ClaimTypes.Name,
            ClaimTypes.Role);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
        if (!string.IsNullOrEmpty(user.UserName))
        {
            identity.AddClaim(new Claim(ClaimTypes.Name, user.UserName));
        }

        if (!string.IsNullOrEmpty(user.Email))
        {
            identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
        }

        if (!string.IsNullOrEmpty(user.SecurityStamp))
        {
            identity.AddClaim(new Claim(identityOptions.Value.ClaimsIdentity.SecurityStampClaimType, user.SecurityStamp));
        }

        var selectedPropertyId = httpContextAccessor.HttpContext is { } http
            ? ActivePropertyCookie.ResolveSelection(http)
            : null;
        var snapshot = await new AccessSnapshotService(authorizationStore)
            .GetSnapshotAsync(user.Id, selectedPropertyId, CancellationToken.None);

        if (snapshot.MembershipId is Guid membershipId)
        {
            identity.AddClaim(new Claim(AuthorizationClaims.MembershipId, membershipId.ToString()));
        }

        if (snapshot.OrganizationId is Guid organizationId)
        {
            identity.AddClaim(new Claim(AuthorizationClaims.OrganizationId, organizationId.ToString()));
        }

        if (snapshot.PropertyId is Guid propertyId)
        {
            identity.AddClaim(new Claim(AuthorizationClaims.PropertyId, propertyId.ToString()));
        }

        if (snapshot.ScopeType is AuthorizationScopeType scope)
        {
            identity.AddClaim(new Claim(AuthorizationClaims.ScopeType, scope.ToString()));
        }

        if (snapshot.EmployeeId is Guid employeeId)
        {
            identity.AddClaim(new Claim(AuthorizationClaims.EmployeeId, employeeId.ToString()));
        }

        foreach (var permission in snapshot.Permissions)
        {
            identity.AddClaim(new Claim(AuthorizationClaims.Permission, permission));
        }

        return new ClaimsPrincipal(identity);
    }
}
