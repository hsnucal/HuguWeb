namespace HuGuWeb.Api.Authorization;

public static class HrMovementPermissions
{
    public const string ClaimType = "permission";
    public const string Read = "hr.movements.read";
    public const string Manage = "hr.movements.manage";

    /// <summary>
    /// Reserved for a future approval workflow. Catalogued only; not granted to HR templates in HR-08A.
    /// </summary>
    public const string Approve = "hr.movements.approve";
}
