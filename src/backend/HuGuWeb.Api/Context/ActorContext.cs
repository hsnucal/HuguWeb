using HuGuWeb.Api.Authorization;

namespace HuGuWeb.Api.Context;

/// <summary>
/// Request-scoped actor. Owned by the API/application boundary, not Domain entities.
/// </summary>
public sealed record ActorContext(
    string UserId,
    Guid? EmployeeId,
    Guid OrganizationId,
    Guid? PropertyId,
    Guid MembershipId,
    AuthorizationScopeType? ScopeType,
    DateTimeOffset OccurredAtUtc);
