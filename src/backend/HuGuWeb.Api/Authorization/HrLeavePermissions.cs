namespace HuGuWeb.Api.Authorization;

public static class HrLeavePermissions
{
    public const string ClaimType = "permission";
    public const string Read = "hr.leave.read";
    public const string Manage = "hr.leave.manage";
    /// <summary>Self-service create &amp; withdraw. Assign via <c>employee-leave-self-service</c> role / role admin.</summary>
    public const string Request = "hr.leave.request";
    /// <summary>Department-stage approve/reject. Requires AUTH-02 department scope. Seeded on HR templates; department bundle via DepartmentSchedulerPermissions.</summary>
    public const string Approve = "hr.leave.approve";
}
