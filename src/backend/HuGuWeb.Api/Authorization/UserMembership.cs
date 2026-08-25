namespace HuGuWeb.Api.Authorization;

public sealed class UserMembership
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public Guid? PropertyId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }

    public AuthorizationScopeType ScopeType =>
        PropertyId is null ? AuthorizationScopeType.Organization : AuthorizationScopeType.Property;

    public ICollection<UserRoleAssignment> RoleAssignments { get; set; } = [];
}
