namespace HuGuWeb.Api.Context;

using System.Security.Claims;
using HuGuWeb.Api.Authorization;

public interface IRequestActorContext
{
    ActorContext? Current { get; }
}

public sealed class RequestActorContext(IHttpContextAccessor httpContextAccessor, TimeProvider time) : IRequestActorContext
{
    public ActorContext? Current
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId)
                || !TryGuid(user, AuthorizationClaims.OrganizationId, out var organizationId)
                || !TryGuid(user, AuthorizationClaims.MembershipId, out var membershipId))
            {
                return null;
            }

            Guid? employeeId = TryGuid(user, AuthorizationClaims.EmployeeId, out var linked)
                ? linked
                : null;
            Guid? propertyId = ActiveWorkplaceResolution.ResolvePropertyId(httpContextAccessor.HttpContext);
            AuthorizationScopeType? scope = Enum.TryParse<AuthorizationScopeType>(
                user.FindFirstValue(AuthorizationClaims.ScopeType),
                out var parsed)
                ? parsed
                : null;

            return new ActorContext(
                userId,
                employeeId,
                organizationId,
                propertyId,
                membershipId,
                scope,
                time.GetUtcNow());
        }
    }

    private static bool TryGuid(ClaimsPrincipal user, string type, out Guid value)
    {
        var raw = user.FindFirstValue(type);
        return Guid.TryParse(raw, out value) && value != Guid.Empty;
    }
}
