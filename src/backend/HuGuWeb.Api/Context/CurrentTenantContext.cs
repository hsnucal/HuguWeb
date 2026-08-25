namespace HuGuWeb.Api.Context;

using System.Security.Claims;
using HuGuWeb.Api.Authorization;

public sealed class CurrentTenantContext(IHttpContextAccessor httpContextAccessor) : ICurrentTenantContext
{
    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public Guid? OrganizationId => TryGuid(AuthorizationClaims.OrganizationId);

    public Guid? MembershipId => TryGuid(AuthorizationClaims.MembershipId);

    public Guid? PropertyId => TryGuid(AuthorizationClaims.PropertyId);

    public AuthorizationScopeType? ScopeType =>
        Enum.TryParse<AuthorizationScopeType>(User?.FindFirstValue(AuthorizationClaims.ScopeType), out var scope)
            ? scope
            : null;

    public bool HasOrganization => OrganizationId is Guid id && id != Guid.Empty;

    public bool HasProperty => PropertyId is Guid id && id != Guid.Empty;

    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    private Guid? TryGuid(string claimType)
    {
        var raw = User?.FindFirstValue(claimType);
        return Guid.TryParse(raw, out var value) && value != Guid.Empty ? value : null;
    }
}
