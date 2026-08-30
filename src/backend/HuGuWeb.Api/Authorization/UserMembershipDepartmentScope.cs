namespace HuGuWeb.Api.Authorization;

/// <summary>
/// Narrowing department scope under a Property <see cref="UserMembership"/>.
/// Zero rows on a membership = Property-wide (no department restriction).
/// Organization-wide memberships must not carry department scopes.
/// </summary>
public sealed class UserMembershipDepartmentScope
{
    public Guid Id { get; set; }
    public Guid UserMembershipId { get; set; }
    public Guid DepartmentId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public UserMembership? Membership { get; set; }
}
