namespace HuGuWeb.Api.Authorization;

public static class HrAttendancePermissions
{
    public const string ClaimType = "permission";
    public const string Read = "hr.attendance.read";
    public const string Manage = "hr.attendance.manage";

    /// <summary>
    /// Reserved for a future period-lock slice. Catalogued only; not granted to HR or department roles.
    /// </summary>
    public const string Close = "hr.attendance.close";
}
