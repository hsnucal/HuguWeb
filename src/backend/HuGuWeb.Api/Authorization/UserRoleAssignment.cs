namespace HuGuWeb.Api.Authorization;

public sealed class UserRoleAssignment
{
    public Guid Id { get; set; }
    public Guid MembershipId { get; set; }
    public Guid RoleId { get; set; }
    public UserMembership? Membership { get; set; }
    public AuthorizationRole? Role { get; set; }
}
