using HuGuWeb.Api.Authorization;

namespace HuGuWeb.Api.Context;

/// <summary>
/// Explicit server-side tenant/workplace for the current HTTP request.
/// Organization-scoped requests may have a null PropertyId.
/// Property-scoped domains must require <see cref="HasProperty"/>.
/// </summary>
public interface ICurrentTenantContext
{
    string? UserId { get; }
    Guid? OrganizationId { get; }
    Guid? MembershipId { get; }
    Guid? PropertyId { get; }
    AuthorizationScopeType? ScopeType { get; }
    bool HasOrganization { get; }
    bool HasProperty { get; }
}
