namespace HuGuWeb.Api.Authorization;

public sealed class RolePermission
{
    public Guid RoleId { get; set; }
    public string PermissionCode { get; set; } = string.Empty;
    public AuthorizationRole? Role { get; set; }
}
