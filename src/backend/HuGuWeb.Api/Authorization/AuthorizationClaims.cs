namespace HuGuWeb.Api.Authorization;

public static class AuthorizationClaims
{
    public const string Permission = WorkforcePermissions.ClaimType;
    public const string MembershipId = "membership_id";
    public const string OrganizationId = "organization_id";
    public const string PropertyId = "property_id";
    public const string ScopeType = "authorization_scope";
    public const string EmployeeId = "employee_id";
}
