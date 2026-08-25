namespace HuGuWeb.Api.Authorization;

public sealed class AuthorizationRole
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public AuthorizationScopeType ScopeType { get; set; }
    public bool IsSystemTemplate { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<RolePermission> Permissions { get; set; } = [];
    public ICollection<UserRoleAssignment> Assignments { get; set; } = [];
}
