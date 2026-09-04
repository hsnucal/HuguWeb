using System.Security.Claims;
using HuGuWeb.Api.Context;

namespace HuGuWeb.Api.Authorization;

/// <summary>
/// Single request operational Property for HTTP.
/// Membership claims answer who/where the user is allowed to be;
/// <see cref="ActivePropertyCookie"/> is the explicit org-wide selection.
/// Property-scoped membership cannot be overridden by cookie contents.
/// </summary>
public static class ActiveWorkplaceResolution
{
    public static Guid? ResolvePropertyId(HttpContext? httpContext)
    {
        var user = httpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        if (IsPropertyScoped(user)
            && TryGuid(user, AuthorizationClaims.PropertyId, out var membershipProperty))
        {
            return membershipProperty;
        }

        if (httpContext is not null)
        {
            var selected = ActivePropertyCookie.ResolveSelection(httpContext);
            if (selected is Guid selectedId)
            {
                return selectedId;
            }
        }

        return TryGuid(user, AuthorizationClaims.PropertyId, out var ticketProperty)
            ? ticketProperty
            : null;
    }

    public static bool IsPropertyScoped(ClaimsPrincipal user) =>
        string.Equals(
            user.FindFirstValue(AuthorizationClaims.ScopeType),
            nameof(AuthorizationScopeType.Property),
            StringComparison.Ordinal);

    private static bool TryGuid(ClaimsPrincipal user, string type, out Guid value)
    {
        var raw = user.FindFirstValue(type);
        return Guid.TryParse(raw, out value) && value != Guid.Empty;
    }
}
