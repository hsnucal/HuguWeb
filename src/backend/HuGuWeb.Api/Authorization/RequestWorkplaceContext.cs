using HuGuWeb.Workforce.Application;
using Microsoft.Extensions.Hosting;
using System.Security.Claims;

namespace HuGuWeb.Api.Authorization;

public sealed class RequestWorkplaceContext(
    IHttpContextAccessor httpContextAccessor,
    IHostEnvironment environment) : IWorkplaceContext
{
    public Guid OrganizationId => Resolve().OrganizationId;

    public Guid PropertyId => Resolve().PropertyId;

    public bool HasOrganization => OrganizationId != Guid.Empty;

    public bool HasProperty => PropertyId != Guid.Empty;

    public bool IsConfigured => HasOrganization;

    private (Guid OrganizationId, Guid PropertyId) Resolve()
    {
        var http = httpContextAccessor.HttpContext;
        var user = http?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            if (environment.IsDevelopment() && http is null)
            {
                return (Guid.Empty, Guid.Empty);
            }

            return (Guid.Empty, Guid.Empty);
        }

        if (!TryGuid(user, AuthorizationClaims.OrganizationId, out var organizationId))
        {
            return (Guid.Empty, Guid.Empty);
        }

        var propertyId = ActiveWorkplaceResolution.ResolvePropertyId(http) ?? Guid.Empty;
        return (organizationId, propertyId);
    }

    private static bool TryGuid(System.Security.Claims.ClaimsPrincipal user, string type, out Guid value)
    {
        var raw = user.FindFirstValue(type);
        return Guid.TryParse(raw, out value) && value != Guid.Empty;
    }
}
