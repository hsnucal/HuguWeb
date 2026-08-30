using System.Security.Claims;

namespace HuGuWeb.Api.Authorization;

/// <summary>
/// Resolves department narrowing for department-aware authorization (e.g. schedule).
/// Null return = unrestricted within the membership's Property/Organization workplace.
/// Zero <see cref="UserMembershipDepartmentScope"/> rows on a Property membership = Property-wide.
/// </summary>
public sealed class MembershipDepartmentAccess(IAuthorizationStore store)
{
    public async Task<IReadOnlySet<Guid>?> GetAllowedDepartmentsAsync(
        Guid? membershipId,
        CancellationToken cancellationToken)
    {
        if (membershipId is null)
        {
            return null;
        }

        var membership = await store.GetMembershipAsync(membershipId.Value, cancellationToken);
        if (membership is null || !membership.IsActive)
        {
            return null;
        }

        // Organization-wide memberships are not department-narrowed.
        if (membership.PropertyId is null)
        {
            return null;
        }

        if (membership.DepartmentScopes.Count == 0)
        {
            return null;
        }

        return membership.DepartmentScopes.Select(item => item.DepartmentId).ToHashSet();
    }

    public static Guid? MembershipIdFromClaims(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(AuthorizationClaims.MembershipId), out var id) ? id : null;
}
