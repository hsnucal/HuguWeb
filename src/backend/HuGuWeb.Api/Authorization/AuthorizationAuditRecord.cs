namespace HuGuWeb.Api.Authorization;

public sealed class AuthorizationAuditRecord
{
    public Guid Id { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string? ActorUserId { get; set; }
    public Guid? ActorOrganizationId { get; set; }
    public Guid? ActorPropertyId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? SubjectUserId { get; set; }
    public Guid? MembershipId { get; set; }
    public Guid? RoleId { get; set; }
    public string? PermissionCode { get; set; }
    public string? Details { get; set; }
}

public static class AuthorizationAuditActions
{
    public const string MembershipCreated = "membership-created";
    public const string MembershipDeactivated = "membership-deactivated";
    public const string MembershipActivated = "membership-activated";
    public const string MembershipDepartmentScopesChanged = "membership-department-scopes-changed";
    public const string RoleAssigned = "role-assigned";
    public const string RoleRemoved = "role-removed";
    public const string RolePermissionChanged = "role-permission-changed";
    public const string UserCreated = "user-created";
    public const string EmployeeLinked = "employee-linked";
}
